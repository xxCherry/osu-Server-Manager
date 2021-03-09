using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace osu__Server_Manager
{
    public class ServerInfo
    {
        public string Name;
        public string EndPoint;

        public ServerInfo(string name, string endpoint)
        {
            Name = name;
            EndPoint = endpoint;
        }
    }

    public static class Config
    {
        public static string Path =
            System.IO.Path.Combine(Environment.GetEnvironmentVariable("LocalAppdata") ?? string.Empty, "config.osm");

        private static Dictionary<string, object> _config = new Dictionary<string, object>()
        {
            ["Servers"] = ""
        };

        public static List<ServerInfo> Servers = new();

        public static T GetValue<T>(string key)
        {
            if (!File.Exists(Path)) 
                return default;

            var configFile = File.ReadAllLines(Path);
            foreach (var line in configFile)
            {
                var kvp = line.Split('=');
                var keyParsed = kvp[0];
                var valueParsed = kvp[1];

                if (keyParsed != key)
                    continue;

                return (T)Convert.ChangeType(valueParsed, typeof(T));
            }

            return default;
        }

        public static void SetValue<T>(string key, T value)
        {
            if (!File.Exists(Path))
                return;

            _config[key] = value;

            SaveConfig();
            LoadConfig();
        }

        public static void LoadConfig()
        {
            if (!File.Exists(Path))
                return;

            _config["Servers"] = GetValue<string>("Servers");

            Servers = JsonConvert.DeserializeObject<List<ServerInfo>>((string)_config["Servers"]);
        }

        public static void SaveConfig()
        {
            if (!File.Exists(Path))
                return;

            var builder = new StringBuilder();

            foreach (var line in _config)
            {
                builder.AppendLine($"{line.Key}={line.Value}");
            }

            File.WriteAllText(Path, builder.ToString());
        }

        public static bool TryCreate()
        {
            if (File.Exists(Path))
                return false;

            var builder = new StringBuilder();

            foreach (var line in _config)
                builder.AppendLine($"{line.Key}={line.Value}");

            File.WriteAllText(Path, builder.ToString());

            return true;
        }
    }
}
