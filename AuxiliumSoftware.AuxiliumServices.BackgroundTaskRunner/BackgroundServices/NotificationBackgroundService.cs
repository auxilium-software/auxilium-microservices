using AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Interfaces;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging.Models;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
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
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationBackgroundService> _logger;

        private IChannel? _channel;
        private const string QueueName = "email.notifications";
        private const string BindingPattern = "email.#";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public NotificationBackgroundService(
            IRabbitMqConnectionManager connectionManager,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationBackgroundService> logger
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

            await _channel.ExchangeDeclareAsync(
                exchange: _connectionManager.Configuration.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

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

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                // declared outside try so the catch block can access them for logging
                string? json = null;
                EmailQueueMessage? message = null;

                try
                {
                    json = Encoding.UTF8.GetString(ea.Body.ToArray());

                    _logger.LogDebug(
                        "Received message {MessageId} with routing key '{RoutingKey}'",
                        ea.BasicProperties?.MessageId, ea.RoutingKey);

                    message = JsonSerializer.Deserialize<EmailQueueMessage>(json, JsonOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Failed to deserialise message {MessageId}, nacking",
                            ea.BasicProperties?.MessageId);
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false,
                            cancellationToken: stoppingToken);
                        return;
                    }

                    switch (message.RoutingKey)
                    {
                        case "email.send":
                            await HandleSendEmailAsync(ea.BasicProperties?.MessageId, json, message, stoppingToken);
                            break;
                        default:
                            _logger.LogWarning(
                                "Unknown routing key '{RoutingKey}' on message {MessageId}, nacking",
                                message.RoutingKey, ea.BasicProperties?.MessageId);
                            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false,
                                cancellationToken: stoppingToken);
                            return;
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process message {MessageId}, nacking (requeue=false)",
                        ea.BasicProperties?.MessageId);

                    await LogFailedActionAsync(ea, json, message, ex, stoppingToken);

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

            while (!stoppingToken.IsCancellationRequested && _channel.IsOpen)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        private async Task HandleSendEmailAsync(
            string? rabbitMessageId, string json, EmailQueueMessage message, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuxiliumDbContext>();
            var templateRenderer = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var targetUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == message.TargetUserId, cancellationToken);

            if (targetUser == null)
            {
                _logger.LogWarning(
                    "User {UserId} not found for message {MessageId}, skipping",
                    message.TargetUserId, rabbitMessageId);
                return;
            }

            var locale = targetUser.LanguagePreference ?? "en-GB";
            message.TemplateData.TryAdd("display_name", targetUser.FullName ?? targetUser.EmailAddress);

            var htmlBody = templateRenderer.Render(message.TemplateName, locale, message.TemplateData);
            var txtBody = "Emails are currently not supported in plain text format.";
            var subject = templateRenderer.TranslateSubject(message.Subject, locale);

            await emailService.SendAsync(
                to: targetUser.EmailAddress,
                subject: subject,
                htmlBody: htmlBody,
                txtBody: txtBody,
                cancellationToken: cancellationToken
            );

            db.Add(new LogSystemMessageQueueSentEmailEntityModel
            {
                Id = UUIDUtilities.GenerateV5(Common.Enumerators.DatabaseObjectTypeEnum.Log_SystemMessageQueue_EmailSent_EventEntry),
                CreatedAt = DateTime.UtcNow,

                MessageId = message.MessageId,
                MessageCreatedAt = message.CreatedAt,
                MessageCorrelationId = message.CorrelationId,
                MessageRoutingKey = message.RoutingKey,
                MessageJson = json,

                EmailRecipientAddress = targetUser.EmailAddress,
                EmailRecipientName = targetUser.FullName ?? targetUser.EmailAddress,
                EmailLanguage = locale,
                EmailTemplate = message.TemplateName,
                EmailSubject = subject,
                EmailBodyHtml = htmlBody,
                EmailBodyTxt = txtBody
            });

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Email sent to {To} via template '{Template}' (locale: {Locale}, message {MessageId})",
                targetUser.EmailAddress, message.TemplateName, locale, rabbitMessageId);
        }

        private async Task LogFailedActionAsync(
            BasicDeliverEventArgs ea, string? json, EmailQueueMessage? message, Exception ex, CancellationToken cancellationToken)
        {
            // wrapped in its own try/catch - a logging failure must never swallow the nack
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AuxiliumDbContext>();

                var messageId = Guid.TryParse(ea.BasicProperties?.MessageId, out var parsed)
                    ? parsed
                    : Guid.Empty;

                var messageCreatedAt = ea.BasicProperties?.Timestamp is { UnixTime: > 0 } ts
                    ? DateTimeOffset.FromUnixTimeSeconds(ts.UnixTime).UtcDateTime
                    : DateTime.UtcNow;

                db.Add(new LogSystemMessageQueueFailedActionEntityModel
                {
                    Id = UUIDUtilities.GenerateV5(Common.Enumerators.DatabaseObjectTypeEnum.Log_SystemMessageQueue_FailedAction_EventEntry),
                    CreatedAt = DateTime.UtcNow,

                    MessageId = messageId,
                    MessageCreatedAt = messageCreatedAt,
                    MessageCorrelationId = ea.BasicProperties?.CorrelationId ?? string.Empty,
                    MessageRoutingKey = message?.RoutingKey ?? ea.RoutingKey,
                    MessageJson = json ?? string.Empty,

                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    ExceptionMessage = ex.Message,
                    ExceptionStackTrace = ex.StackTrace ?? string.Empty
                });

                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception logEx)
            {
                _logger.LogCritical(logEx, "Failed to write failed-action log to the database");
            }
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
