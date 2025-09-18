using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AuxiliumMicroservices.Common.Utilities
{
    internal static class ConfigurationUtilities
    {
        internal static string ConfigurationFileLocation;

        private static Dictionary<object, object>? GetConfiguration()
        {
            if (!File.Exists(ConfigurationFileLocation))
            {
                throw new NullReferenceException("Configuration file does not exist.");
            }

            string yamlContent = File.ReadAllText(ConfigurationFileLocation);

            var deserializer = new DeserializerBuilder()
                .Build();

            return deserializer.Deserialize<Dictionary<object, object>>(yamlContent);
        }

        public static object GetObject(params string[] path)
        {
            var config = GetConfiguration();

            object current = config;

            foreach (var key in path)
            {
                if (current is Dictionary<object, object> dict)
                {
                    if (dict.TryGetValue(key, out var value))
                    {
                        current = value!;
                    }
                    else
                    {
                        throw new NullReferenceException("Path leads to nowhere");
                    }
                }
                else
                {
                    throw new NullReferenceException("Path leads to nowhere");
                }
            }

            return current;
        }

        public static string GetString(params string[] path)
        {
            return GetObject(path)?.ToString();
        }
        public static int GetInteger(params string[] path)
        {
            return System.Convert.ToInt32(GetObject(path));
        }
    }
}
