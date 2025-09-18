using AuxiliumMicroservices.Common.Utilities;
using CouchDB.Driver;

namespace AuxiliumMicroservices.Common.ServiceInteractions
{
    internal static class CouchDBInteractions
    {
        internal static string GenerateConnectionString()
        {
            string protocol = ConfigurationUtilities.GetString("Databases", "CouchDB", "Protocol");
            string hostname = ConfigurationUtilities.GetString("Databases", "CouchDB", "Host");
            int    port     = ConfigurationUtilities.GetInteger("Databases", "CouchDB", "Port");
            string username = ConfigurationUtilities.GetString("Databases", "CouchDB", "Username");
            string password = ConfigurationUtilities.GetString("Databases", "CouchDB", "Password");

            if (string.IsNullOrEmpty(username))
                return $"{protocol}://{hostname}:{port}/";

            return $"{protocol}://{username}:{password}@{hostname}:{port}/";
        }

        internal static async Task<bool> Test()
        {
            try
            {
                var connectionString = GenerateConnectionString();

                await using var client = new CouchClient(connectionString, builder => {
                });
                return await client.IsUpAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CouchDB test failed: {ex.Message}");
                return false;
            }
        }
    }
}
