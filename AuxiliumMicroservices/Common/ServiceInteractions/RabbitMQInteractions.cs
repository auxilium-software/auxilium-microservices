using AuxiliumMicroservices.Common.Utilities;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.ServiceInteractions
{
    internal static class RabbitMQInteractions
    {
        internal static ConnectionFactory GenerateConnectionFactory()
        {
            string hostname = ConfigurationUtilities.GetString("Databases", "RabbitMQ", "Host");
            int    port     = ConfigurationUtilities.GetInteger("Databases", "RabbitMQ", "Port");
            string username = ConfigurationUtilities.GetString("Databases", "RabbitMQ", "Username");
            string password = ConfigurationUtilities.GetString("Databases", "RabbitMQ", "Password");
            string vhost    = ConfigurationUtilities.GetString("Databases", "RabbitMQ", "VirtualHost");

            return new ConnectionFactory
            {
                HostName = hostname,
                Port = port,
                UserName = username,
                Password = password,
                VirtualHost = vhost
            };
        }

        internal static async Task<bool> Test()
        {
            try
            {
                var factory = GenerateConnectionFactory();

                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();


                await channel.QueueDeclareAsync(queue: "", durable: false, exclusive: false, autoDelete: false, arguments: null);


                return channel.IsOpen;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ test failed: {ex.Message}");
                return false;
            }
        }
    }
}
