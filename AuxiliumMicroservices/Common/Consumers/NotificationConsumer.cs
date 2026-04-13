using System;
using System.Threading;
using System.Threading.Tasks;
using AuxiliumMicroservices.Common.MessageFormats;
using AuxiliumMicroservices.Common.Utilities;

namespace AuxiliumMicroservices.Common.Consumers
{
    internal class NotificationConsumer
    {
        internal static async Task<bool> NotificationConsumerRunner(string message)
        {
            try
            {
                Logger.Info("Notifications", $"Processing message: {message}");
                Email decodedMessage = new(DefinedObject.ParseJsonToDictionary(message));

                Logger.Info("Notifications", "Message processed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Notifications", $"Error processing message: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
}
