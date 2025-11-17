using AuxiliumMicroservices.Common.DataClasses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.Utilities
{
    internal class ConsumerController
    {
        private static Dictionary<string, ConsumerDetailsWrapper> consumers = new();

        internal static void AddConsumer(string consumerName, Func<CancellationToken, Task<bool>> consumerFactory)
        {
            ConsumerDetailsWrapper temp = new(
                consumerName,
                consumerFactory
            );
            consumers.Add(consumerName, temp);
        }

        internal static async Task StartConsumers(CancellationToken cancellationToken)
        {
            var tasks = new List<Task<bool>>();

            foreach (var kvp in consumers)
            {
                Console.WriteLine($"Starting consumer: {kvp.Key}");
                tasks.Add(kvp.Value.StartTask(cancellationToken));
            }

            tasks.Add(StateTableWriter.OutputStatusTable(cancellationToken));

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Consumers stopped gracefully.");
            }
        }

        internal static Dictionary<string, ConsumerDetailsWrapper> GetConsumers()
        {
            return consumers;
        }
    }
}
