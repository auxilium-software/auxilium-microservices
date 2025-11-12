using AuxiliumMicroservices.Common.Utilities;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;
using System;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.ServiceInteractions
{
    internal static class ClickHouseInteractions
    {
        internal static string GenerateConnectionString()
        {
            string hostname = ConfigurationUtilities.GetString("Databases", "ClickHouse", "Host");
            int port = ConfigurationUtilities.GetInteger("Databases", "ClickHouse", "HTTPPort");
            string username = ConfigurationUtilities.GetString("Databases", "ClickHouse", "Username");
            string password = ConfigurationUtilities.GetString("Databases", "ClickHouse", "Password");
            string database = ConfigurationUtilities.GetString("Databases", "ClickHouse", "Database");

            return $"Host={hostname};Port={port};Database={database};Username={username};Password={password};";
        }

        internal static async Task<bool> Test()
        {
            try
            {
                var connectionString = GenerateConnectionString();

                await using var connection = new ClickHouseConnection(connectionString);
                await connection.OpenAsync();

                var query = "SELECT 1";
                await using var command = connection.CreateCommand();
                command.CommandText = query;
                var result = await command.ExecuteScalarAsync();

                return result?.ToString() == "1";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ClickHouse test failed: {ex.Message}");
                return false;
            }
        }
    }
}
