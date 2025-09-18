using AuxiliumMicroservices.Common.ServiceInteractions;
using AuxiliumMicroservices.Common.Utilities;
using System;
using System.Threading.Tasks;

namespace AuxiliumMicroservices
{
    internal class Program
    {
        private static readonly string[] Services =
            { "CouchDB", "MariaDB", "RabbitMQ", "Redis" };

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

        private static async Task Preflight()
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
                    "Redis" => RedisInteractions.Test,
                    _ => throw new ArgumentOutOfRangeException()
                };

                await CheckServiceAsync(service, testFunc, i + 1, total);
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine(@"
                     _ _ _                       _______        _          _____                             
     /\             (_) (_)                     |__   __|      | |        |  __ \                            
    /  \  _   ___  ___| |_ _   _ _ __ ___          | | __ _ ___| | __     | |__) |   _ _ __  _ __   ___ _ __ 
   / /\ \| | | \ \/ / | | | | | | '_ ` _ \         | |/ _` / __| |/ /     |  _  / | | | '_ \| '_ \ / _ \ '__|
  / ____ \ |_| |>  <| | | | |_| | | | | | |        | | (_| \__ \   <      | | \ \ |_| | | | | | | |  __/ |   
 /_/    \_\__,_/_/\_\_|_|_|\__,_|_| |_| |_|        |_|\__,_|___/_|\_\     |_|  \_\__,_|_| |_|_| |_|\___|_|   
");

            string configPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    configPath = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(configPath))
            {
                ConsoleWriting.CatastrophicFail("No configuration file provided. Use --config <path>\n");
                Environment.Exit(1);
            }

            ConfigurationUtilities.ConfigurationFileLocation = configPath;
            ConsoleWriting.Debug($"Using config file: {configPath}\n");

            ConsoleWriting.Debug("Starting preflight...\n");

            try
            {
                await Preflight();
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
