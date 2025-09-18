using AuxiliumMicroservices.Common.Utilities;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.ServiceInteractions
{
    internal static class RedisInteractions
    {
        internal static string GenerateConnectionString()
        {
            string hostname = ConfigurationUtilities.GetString("Databases", "Redis", "Host");
            int    port     = ConfigurationUtilities.GetInteger("Databases", "Redis", "Port");
            string password = ConfigurationUtilities.GetString("Databases", "Redis", "Password");

            if (!string.IsNullOrEmpty(password))
            {
                return $"{hostname}:{port},password={password}";
            }

            return $"{hostname}:{port}";
        }

        internal static async Task<bool> Test()
        {
            try
            {
                var connection = await ConnectionMultiplexer.ConnectAsync(GenerateConnectionString());
                var db = connection.GetDatabase();

                var pong = await db.PingAsync();
                return pong.TotalMilliseconds >= 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
