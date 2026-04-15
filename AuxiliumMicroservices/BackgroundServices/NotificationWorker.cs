using AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Interfaces;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Models;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.BackgroundServices
{
    public class NotificationWorker : BackgroundService
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationWorker> _logger;

        private IChannel? _channel;
        private const string QueueName = "email.notifications";
        private const string BindingPattern = "email.#";

        public NotificationWorker(
            IRabbitMqConnectionManager connectionManager,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationWorker> logger
        )
        {
            _connectionManager = connectionManager;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification worker starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification worker encountered an error, restarting in 5s...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Notification worker stopped");
        }

        private async Task ConsumeAsync(CancellationToken stoppingToken)
        {
            var connection = await _connectionManager.GetConnectionAsync(stoppingToken);
            _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // declare exchange (must match producer - topic, durable)
            await _channel.ExchangeDeclareAsync(
                exchange: _connectionManager.Configuration.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // declare and bind the queue
            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: _connectionManager.Configuration.ExchangeName,
                routingKey: BindingPattern,
                cancellationToken: stoppingToken);

            // one message at a time - don't fetch more until we've acked
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    await HandleMessageAsync(ea, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false,
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process message {MessageId}, nacking (requeue=false)",
                        ea.BasicProperties?.MessageId);

                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false,
                        cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Subscribed to queue '{Queue}' with binding '{Binding}'",
                QueueName, BindingPattern);

            // keep alive until cancelled or channel drops
            while (!stoppingToken.IsCancellationRequested && _channel.IsOpen)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            _logger.LogDebug(
                "Received message {MessageId} with routing key '{RoutingKey}'",
                ea.BasicProperties?.MessageId, ea.RoutingKey);

            var message = JsonSerializer.Deserialize<EmailQueueMessage>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialise message {MessageId}, skipping",
                    ea.BasicProperties?.MessageId);
                return;
            }

            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AuxiliumDbContext>();
            var templateRenderer = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // resolve user from the database
            var targetUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == message.UserId, cancellationToken);

            if (targetUser == null)
            {
                _logger.LogWarning(
                    "User {UserId} not found for message {MessageId}, skipping",
                    message.UserId, ea.BasicProperties?.MessageId);
                return;
            }

            var locale = targetUser.LanguagePreference ?? "en-GB";

            // inject user fields into template data (won't overwrite if the producer set them explicitly)
            message.TemplateData.TryAdd("display_name", targetUser.FullName ?? targetUser.EmailAddress);

            var htmlBody = templateRenderer.Render(message.TemplateName, locale, message.TemplateData);
            var subject = templateRenderer.TranslateSubject(message.Subject, locale);

            await emailService.SendAsync(
                to: targetUser.EmailAddress,
                subject: subject,
                htmlBody: htmlBody,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "Email sent to {To} via template '{Template}' (locale: {Locale}, message {MessageId})",
                targetUser.EmailAddress, message.TemplateName, locale, ea.BasicProperties?.MessageId
            );
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is { IsOpen: true })
            {
                await _channel.CloseAsync(cancellationToken);
                _channel.Dispose();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
