using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.Utilities
{
    internal static class StateTableWriter
    {
        private const int queueNameWidth = 30;
        private const int statusWidth = 20;

        private static int lastTotalLines = 0;
        private static bool firstRender = true;

        internal static async Task<bool> OutputStatusTable(CancellationToken cancellationToken)
        {
            string separator = new('-', queueNameWidth + (statusWidth * 3) + (3 * 3) + 4);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!firstRender)
                {
                    Console.SetCursorPosition(0, Console.CursorTop - lastTotalLines);
                }

                Console.WriteLine(separator);
                Console.WriteLine($"| {"Queue",-queueNameWidth} | {"Successful Jobs",-statusWidth} | {"Failed Jobs",-statusWidth} | {"Total Jobs",-statusWidth} |");
                Console.WriteLine(separator);

                int currentRowCount = 0;
                foreach (var (QueueName, consumerDetails) in ConsumerController.GetConsumers())
                {
                    Console.Write($"| {QueueName,-queueNameWidth} | ");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{consumerDetails.GetSuccessfulJobs(),-statusWidth}");
                    Console.ResetColor();
                    Console.Write(" | ");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{consumerDetails.GetFailedJobs(),-statusWidth}");
                    Console.ResetColor();
                    Console.Write(" | ");

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"{consumerDetails.GetTotalJobs(),-statusWidth}");
                    Console.ResetColor();
                    Console.WriteLine(" |");

                    currentRowCount++;
                }

                Console.WriteLine(separator);

                int currentTotalLines = currentRowCount + 4;

                if (currentTotalLines < lastTotalLines)
                {
                    for (int i = 0; i < (lastTotalLines - currentTotalLines); i++)
                    {
                        Console.WriteLine(new string(' ', separator.Length));
                    }
                }

                lastTotalLines = currentTotalLines;
                firstRender = false;
            }
            return true;
        }
    }
}
