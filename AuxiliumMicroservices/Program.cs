using AuxiliumMicroservices.Common.Consumers;
using AuxiliumMicroservices.Common.ServiceInteractions;
using AuxiliumMicroservices.Common.Utilities;
using System.Threading;

namespace AuxiliumMicroservices
{
    internal class Program
    {
        static async Task<bool> Consumer_Notifications(string message)
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [Notifications] {message}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Notifications] Error: {ex.Message}");
                return false;
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

            ArgumentParsing.SortConfigFileLocation(args);
            await Preflight.Go();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                ConsoleWriting.CatastrophicFail("\nShutdown signal received, stopping consumers...");
            };

            ConsoleWriting.Debug("Starting message consumers...\n");

            ConsumerController.AddConsumer("Notifications",
                ct => NotificationConsumer.NotificationConsumerRunner(ct));

            await ConsumerController.StartConsumers(cts.Token);
        }
    }
}
