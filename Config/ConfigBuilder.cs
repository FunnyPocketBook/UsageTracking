using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProgramTracker.Config
{
    class ConfigBuilder
    {
        private static ConfigBuilder instance;
        public Config Config { get; private set; }
        readonly string configName = "config.json";

        protected ConfigBuilder()
        {
            if (instance == null)
            {
                Config = new Config();
            }
        }

        public static ConfigBuilder Instance()
        {
            if (instance == null)
            {
                instance = new ConfigBuilder();
            }
            return instance;
        }

        public void Save(object obj)
        {
            string jsonOut = JsonConvert.SerializeObject(obj, Formatting.Indented);
            using StreamWriter writer = new StreamWriter(configName);
            writer.WriteLine(jsonOut);
        }

        public void Load()
        {
            if (!File.Exists(configName))
            {
                Save(Config);
            }
            using StreamReader reader = new StreamReader(configName);
            JsonSerializer serializer = new JsonSerializer();
            var deserialized = serializer.Deserialize(reader, typeof(Config)) as Config;
            Config = deserialized ?? Config;
        }
    }
}
