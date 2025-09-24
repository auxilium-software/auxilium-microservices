using AuxiliumMicroservices.Common.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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


        internal static async Task ConsumeMessagesAsync(string queueKey, Func<string, Task<bool>> messageHandler, CancellationToken cancellationToken = default)
        {
            try
            {
                var factory = GenerateConnectionFactory();
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: ConfigurationUtilities.GetString("Databases", "RabbitMQ", "Queues", queueKey),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine($"Received message from queue '{queueKey}': {message}");

                    try
                    {
                        bool success = await messageHandler(message);

                        if (success)
                        {
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                            Console.WriteLine("Message processed successfully");
                        }
                        else
                        {
                            await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                            Console.WriteLine("Message processing failed, requeued");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                        await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: ConfigurationUtilities.GetString("Databases", "RabbitMQ", "Queues", queueKey),
                    autoAck: false, // Manual acknowledgment
                    consumer: consumer);

                Console.WriteLine($"Started consuming messages from queue '{queueKey}'. Press Ctrl+C to stop.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Message consumption cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in message consumption: {ex.Message}");
                throw;
            }
        }

    }
}
