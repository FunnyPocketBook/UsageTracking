using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ProgramTracker.Config;
using System.Threading.Tasks;
using System.Threading;
using ProgramTracker.Helper;
using Microsoft.EntityFrameworkCore.Storage;

namespace ProgramTracker
{
    class ProgramTracker
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }

        private static Stopwatch stopWatch = new Stopwatch();
        private static ConfigBuilder builder = ConfigBuilder.Instance();
        private static DbContext context = new DbContext();
        private static bool running = true;
        private static Program curProgram;
        private static bool debug = true;
        private static ManualResetEvent waitHandle = new ManualResetEvent(false);
        private static bool pause = false;
        private static bool init = false;
        private static bool connected = false;
        private static void Main(string[] args)
        {
            builder.Load();
            Console.WriteLine("Connecting to database...");
            try
            {
                context.Database.Migrate();
                connected = true;
            }
            catch(RetryLimitExceededException ex) // Connecting to database failed after being retried the configured number of times.
            {
                Console.WriteLine("Failed to Connect to database, retry limit exceeded.");
            }
            if (connected == true)
            {
                Console.WriteLine("Connected to database");
                Console.CancelKeyPress += (sender, e) => KeyboardInterrupt();
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
                curProgram = InitProgramCounter(GetActiveProcessFileName());
                stopWatch.Start();

                Task idleTask = Task.Run(() => CheckIdleTime());
                string oldProgramName = curProgram.Name;
                while (running)
                {
                    if (pause)
                    {
                        stopWatch.Stop();
                        SaveProgramInDb();

                        stopWatch = new Stopwatch();
                        stopWatch.Start();
                        curProgram = InitProgramCounter("Idle");

                        waitHandle.WaitOne();

                        stopWatch.Stop();
                        SaveProgramInDb();

                        stopWatch = new Stopwatch();
                        stopWatch.Start();
                        curProgram = InitProgramCounter(GetActiveProcessFileName());
                    }
                    if (!oldProgramName.Equals(curProgram.Name))
                    {
                        stopWatch.Stop();
                        SaveProgramInDb();
                        stopWatch = new Stopwatch();
                        stopWatch.Start();
                        curProgram = InitProgramCounter(GetActiveProcessFileName());
                    }
                    oldProgramName = GetActiveProcessFileName();
                    Thread.Sleep(builder.Config.PollRate);
                    if (debug)
                    {
                        if (!init)
                        {
                            Console.Clear();
                            init = true;
                        }
                        Console.SetCursorPosition(0, 2);
                        foreach (Program p in context.Programs.Where(p => p.User == builder.Config.User).OrderBy(prog => prog.Elapsed).ToList())
                        {
                            Console.WriteLine($"Name: {p.Name}, User: {p.User}, Elapsed: {p.Elapsed}");
                            List<Timerange> timeranges = p.Timeranges;
                            Console.WriteLine("--------");
                        }
                    }
                }
                idleTask.Wait();
            }
        }

        private static void CheckIdleTime()
        {
            TimeSpan oldIdleTime = RetrieveIdleTime();
            while (running)
            {
                TimeSpan curIdleTime = RetrieveIdleTime();
                if (curIdleTime.CompareTo(new TimeSpan(0, builder.Config.IdleTimeMinutes, 0)) > 0
                    && stopWatch.IsRunning && !AudioDetector.IsAnyAudioPlaying() && !pause)
                {
                    if (debug)
                    {
                        Console.WriteLine("Idle");
                    }
                    waitHandle.Reset();
                    pause = true;
                }
                else if (pause && oldIdleTime.CompareTo(curIdleTime) > 0)
                {
                    if (debug)
                    {
                        Console.WriteLine("Resuming");
                    }
                    pause = false;
                    waitHandle.Set();
                }
                oldIdleTime = curIdleTime;
                Thread.Sleep(30000);
            }
        }

        private static void SaveProgramInDb()
        {
            // set end date for old program
            curProgram.Timeranges.Last().End = DateTime.Now;
            curProgram.Elapsed += stopWatch.Elapsed;

            // check if new program exists
            // if exists, create new timerange with startDate as start
            Program program = context.Programs.SingleOrDefault(p => p.Name == curProgram.Name && p.User == curProgram.User);
            if (program != null)
            {
                string newProgramName = GetActiveProcessFileName();
                Program newProgram = context.Programs.SingleOrDefault(p => p.Name == newProgramName && p.User == curProgram.User);
                int newProgramId;
                if (newProgram == null)
                {
                    var addedProgram = context.Add(new Program
                    {
                        Name = newProgramName,
                        User = builder.Config.User
                    });
                    context.SaveChanges();
                    /* 
                     * TODO: Get the ProgramId of the newly added program above. context.Add() is supposed to be synchronous but for some reason,
                     * 
                     * the line below returns null, meaning the program hasn't been added to the DB yet.
                     * 
                     */
                    Program newlyAddedProgram = context.Programs.SingleOrDefault(p => p.Name == newProgramName && p.User == curProgram.User);
                    newProgramId = newlyAddedProgram.ProgramId;
                } else
                {
                    newProgramId = newProgram.ProgramId;
                }
                Timerange currentTimerange = curProgram.Timeranges.Last();
                currentTimerange.NextProgramId = newProgramId;
                program.Timeranges.Add(curProgram.Timeranges.Last());
                program.Elapsed += curProgram.Elapsed;
            }
            // else, create new program with startDate as start
            else
            {
                context.Add(curProgram);
            }
            context.SaveChanges();
        }

        private static string GetActiveProcessFileName()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }

        private static TimeSpan RetrieveIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)LASTINPUTINFO.SizeOf;
            GetLastInputInfo(ref lastInputInfo);

            int elapsedTicks = Environment.TickCount - (int)lastInputInfo.dwTime;

            if (elapsedTicks > 0) { return new TimeSpan(0, 0, 0, 0, elapsedTicks); }
            else { return new TimeSpan(0); }
        }

        private static Program InitProgramCounter(string processName)
        {
            int oldProgramId = -1;
            if (curProgram != null)
            {
                Program oldProgram = context.Programs.SingleOrDefault(p => p.Name == curProgram.Name && p.User == curProgram.User);
                if (oldProgram != null)
                {
                    oldProgramId = oldProgram.ProgramId;
                }
            }
            curProgram = new Program
            {
                Name = processName,
                User = builder.Config.User
            };
            curProgram.Timeranges.Add(
            new Timerange
            {
                Start = DateTime.Now,
                PrevProgramId = oldProgramId
            });
            return curProgram;
        }

        private static void KeyboardInterrupt()
        {
            Console.WriteLine("Keyboard interrupt");
            System.Environment.Exit(1);
        }
        private static void OnProcessExit(object sender, EventArgs e)
        {
            SaveProgramInDb();
            context.SaveChanges();
            context.Dispose();
            running = false;
        }
    }
}
