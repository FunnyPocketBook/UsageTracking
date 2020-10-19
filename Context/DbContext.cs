using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using ProgramTracker.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ProgramTracker
{
    class DbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<Program> Programs { get; set; }
        public DbSet<Timerange> Timeranges { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var configBuilder = ConfigBuilder.Instance();
            configBuilder.Load();
            string[] versions = configBuilder.Config.Version.Split(".");
            options.UseMySql($"Server={configBuilder.Config.Server};Database={configBuilder.Config.Database};User={configBuilder.Config.DbUser};Password={configBuilder.Config.Password}",
                  mySqlOptions => {
                      mySqlOptions.ServerVersion(new Version(int.Parse(versions[0]), int.Parse(versions[1]), int.Parse(versions[2])), ServerType.MySql);
                      mySqlOptions.EnableRetryOnFailure(configBuilder.Config.DbConnectionRetry);
                    });
        }
    }

    class Program
    { 

        public int ProgramId { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public List<Timerange> Timeranges { get; } = new List<Timerange>();
        public TimeSpan Elapsed { get; set; }
    }

    class Timerange
    {
        public int TimerangeId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int ProgramId { get; set; }
        public int PrevProgramId { get; set; }
        public int NextProgramId { get; set; }
    }
}
