using AuxiliumMicroservices.Common.Utilities;
using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.ServiceInteractions
{
    internal static class MariaDBInteractions
    {
        internal static string GenerateConnectionString()
        {
            string hostname = ConfigurationUtilities.GetString("Databases", "MariaDB", "Host");
            int    port     = ConfigurationUtilities.GetInteger("Databases", "MariaDB", "Port");
            string username = ConfigurationUtilities.GetString("Databases", "MariaDB", "Username");
            string password = ConfigurationUtilities.GetString("Databases", "MariaDB", "Password");
            string database = ConfigurationUtilities.GetString("Databases", "MariaDB", "Database");

            return $"Server={hostname};Port={port};Database={database};User ID={username};Password={password};";
        }

        internal static async Task<bool> Test()
        {
            try
            {
                await using var connection = new MySqlConnection(GenerateConnectionString());
                await connection.OpenAsync();

                var query = "SELECT 1;";
                await using var command = new MySqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return result?.ToString() == "1";
            }
            catch
            {
                return false;
            }
        }
    }
}
