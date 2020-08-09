using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ProgramTracker.Config;

namespace ProgramTracker
{
    class ProgramTracker
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);
        private static ConfigBuilder builder = ConfigBuilder.Instance();


        private static void Main(string[] args)
        {
            builder.Load();
            DbContext db = new DbContext();
            db.Database.Migrate();
            Console.CancelKeyPress += (sender, e) => KeyboardInterrupt(db);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Program curProgram = InitProgramCounter();
            string oldProgramName = curProgram.Name;
            while (true)
            {
                if (!oldProgramName.Equals(curProgram.Name))
                {
                    stopWatch.Stop();
                    // set end date for old program
                    curProgram.Timeranges.Last().End = DateTime.Now;
                    curProgram.Elapsed += stopWatch.Elapsed;

                    // check if new program exists
                    // if exists, create new timerange with startDate as start
                    Program program = db.Programs.SingleOrDefault(p => p.Name == curProgram.Name && p.User == curProgram.User);
                    if (program != null)
                    {
                        program.Timeranges.Add(curProgram.Timeranges.Last());
                        program.Elapsed += curProgram.Elapsed;
                        stopWatch = new Stopwatch();
                        stopWatch.Start();
                    }
                    // else, create new program with startDate as start
                    else
                    {
                        db.Add(curProgram);
                        stopWatch = new Stopwatch();
                        stopWatch.Start();
                    }
                    curProgram = InitProgramCounter();
                    db.SaveChanges();
                }
                oldProgramName = GetActiveProcessFileName();
                System.Threading.Thread.Sleep(1000);
                //Console.Clear();
                Console.SetCursorPosition(0, 0);
                foreach (Program p in db.Programs
                    //.Include(prog => prog.Timeranges)
                    .ToList())
                {
                    Console.WriteLine($"Name: {p.Name}, User: {p.User}, Elapsed: {p.Elapsed}");
                    List<Timerange> timeranges = p.Timeranges;
                    /*foreach (Timerange t in timeranges)
                    {
                        Console.WriteLine("Start: {0}, End: {1}", t.Start, t.End);
                    }*/
                    Console.WriteLine("--------");
                }
            }

        }

        static string GetActiveProcessFileName()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }

        private static Program InitProgramCounter()
        {
            Program curProgram = new Program
            {
                Name = GetActiveProcessFileName(),
                User = builder.Config.User
            };
            curProgram.Timeranges.Add(
            new Timerange
            {
                Start = DateTime.Now
            });
            return curProgram;
        }

        static void KeyboardInterrupt(DbContext db)
        {
            Console.WriteLine("Keyboard interrupt");
            db.SaveChanges();
            db.Dispose();
            System.Environment.Exit(1);
        }
    }
}
