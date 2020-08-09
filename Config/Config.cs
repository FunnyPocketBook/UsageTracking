using System;
using System.Collections.Generic;
using System.Text;

namespace ProgramTracker.Config
{
    class Config
    {
        public string Server { get; set; } = "localhost";
        public string Database { get; set; } = "usage_tracking";
        public string DbUser { get; set; } = "root";
        public string Password { get; set; } = "1234";
        public string Version { get; set; } = "10.4.13";
        public string User { get; set; } = $"{Environment.UserName}_{Environment.MachineName}";
    }
}
