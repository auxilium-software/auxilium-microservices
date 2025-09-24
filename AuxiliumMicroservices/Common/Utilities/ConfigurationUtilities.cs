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

        private static Dictionary<object, object> GetDefaultValues()
        {
            return new Dictionary<object, object>
            {
                ["Databases"] = new Dictionary<object, object>
                {
                    ["MariaDB"] = new Dictionary<object, object>
                    {
                        ["Port"] = 3306
                    },
                    ["CouchDB"] = new Dictionary<object, object>
                    {
                        ["Port"] = 5984
                    },
                    ["Redis"] = new Dictionary<object, object>
                    {
                        ["Port"] = 6379,
                        ["ConnectTimeout"] = 5,
                        ["SocketTimeout"] = 5,
                        ["DecodeResponses"] = true,
                        ["RetryOnTimeout"] = true,
                        ["HealthCheckInterval"] = 30
                    },
                    ["RabbitMQ"] = new Dictionary<object, object>
                    {
                        ["Port"] = 5672,
                        ["Heartbeat"] = 600,
                        ["BlockedConnectionTimeout"] = 300
                    }
                },
                ["ReCAPTCHA"] = new Dictionary<object, object>
                {
                    ["ScoreThreshold"] = 0.5
                },
                ["JWT"] = new Dictionary<object, object>
                {
                    ["Algorithm"] = "HS256"
                }
            };
        }

        private static object GetDefaultValue(params string[] path)
        {
            var defaults = GetDefaultValues();
            object current = defaults;

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
                        throw new KeyNotFoundException($"No default value found for path: {string.Join(" -> ", path)}");
                    }
                }
                else
                {
                    throw new KeyNotFoundException($"No default value found for path: {string.Join(" -> ", path)}");
                }
            }
            return current;
        }

        public static object GetObject(params string[] path)
        {
            var config = GetConfiguration();
            object current = config;

            try
            {
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
                            throw new KeyNotFoundException();
                        }
                    }
                    else
                    {
                        throw new KeyNotFoundException();
                    }
                }
                return current;
            }
            catch (KeyNotFoundException)
            {
                try
                {
                    return GetDefaultValue(path);
                }
                catch (KeyNotFoundException)
                {
                    throw new KeyNotFoundException($"Configuration value not found for path: {string.Join(" -> ", path)} (not in config file or defaults)");
                }
            }
        }

        public static string GetString(params string[] path)
        {
            var value = GetObject(path);
            if (value == null)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} is null and cannot be converted to string");
            }
            return value.ToString()!;
        }

        public static int GetInteger(params string[] path)
        {
            var value = GetObject(path);
            if (value == null)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} is null and cannot be converted to int");
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} cannot be converted to int: {value}", ex);
            }
        }

        public static float GetFloat(params string[] path)
        {
            var value = GetObject(path);
            if (value == null)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} is null and cannot be converted to float");
            }

            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} cannot be converted to float: {value}", ex);
            }
        }

        public static double GetDouble(params string[] path)
        {
            var value = GetObject(path);
            if (value == null)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} is null and cannot be converted to double");
            }

            try
            {
                return Convert.ToDouble(value);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} cannot be converted to double: {value}", ex);
            }
        }

        public static bool GetBoolean(params string[] path)
        {
            var value = GetObject(path);
            if (value == null)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} is null and cannot be converted to bool");
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is string stringValue)
            {
                var lowerValue = stringValue.ToLower();
                return lowerValue switch
                {
                    "true" or "1" or "yes" or "on" => true,
                    "false" or "0" or "no" or "off" => false,
                    _ => throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} cannot be converted to bool: '{stringValue}'")
                };
            }

            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException)
            {
                throw new InvalidOperationException($"Configuration value at path {string.Join(" -> ", path)} cannot be converted to bool: {value}", ex);
            }
        }
    }
}