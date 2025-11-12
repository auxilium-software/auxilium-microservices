using AuxiliumMicroservices.Common.ServiceInteractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.Utilities
{
    internal static class Preflight
    {
        private static readonly string[] Services = {
            "CouchDB",
            "MariaDB",
            "RabbitMQ",
        };

        private static async Task CheckServiceAsync(string serviceName, Func<Task<bool>> testFunc, int step, int total)
        {
            ConsoleWriting.Debug($"[ {step}/{total} ] Checking connection to {serviceName + " server...",-20}");

            bool success = await testFunc();
            if (success)
            {
                ConsoleWriting.Success("SUCCESS\n");
            }
            else
            {
                ConsoleWriting.CatastrophicFail("FAILURE\n");
                throw new Exception($"{serviceName} connection failed");
            }
        }

        internal static async Task Go()
        {
            ConsoleWriting.Debug("Starting preflight...\n");
            try
            {
                int total = Services.Length;
                for (int i = 0; i < total; i++)
                {
                    string service = Services[i];
                    Func<Task<bool>> testFunc = service switch
                    {
                        "CouchDB" => CouchDBInteractions.Test,
                        "MariaDB" => MariaDBInteractions.Test,
                        "RabbitMQ" => RabbitMQInteractions.Test,
                    };

                    await CheckServiceAsync(service, testFunc, i + 1, total);
                }
                ConsoleWriting.Success("\nAll services verified successfully.\n");
            }
            catch (Exception ex)
            {
                ConsoleWriting.CatastrophicFail($"\nPreflight checks failed: {ex.Message}\n");
                Environment.Exit(1);
            }
        }
    }
}
