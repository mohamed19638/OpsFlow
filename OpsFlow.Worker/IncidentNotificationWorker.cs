using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Messaging;
using OpsFlow.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OpsFlow.Worker;

public sealed class IncidentNotificationWorker(
    ILogger<IncidentNotificationWorker> logger,
    IOptions<RabbitMqSettings> options) : BackgroundService
{
    private readonly RabbitMqSettings _settings = options.Value;
    private readonly ConcurrentDictionary<Guid, byte> _processedMessages = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            AutomaticRecoveryEnabled = true
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(channelOptions, stoppingToken);
        await RabbitMqTopology.DeclareAsync(channel, _settings, stoppingToken);
        await channel.BasicQosAsync(0, 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                var body = delivery.Body.ToArray();
                if (delivery.RoutingKey == "incidents.created")
                {
                    var message = JsonSerializer.Deserialize<IncidentCreatedMessage>(body)
                        ?? throw new JsonException("Incident-created message was empty.");
                    if (_processedMessages.TryAdd(message.MessageId, 0))
                        logger.LogInformation("NOTIFICATION: New {Severity} incident '{Title}' on service '{ServiceName}' (incident {IncidentId})",
                            message.Severity, message.Title, message.ServiceName, message.IncidentId);
                }
                else if (delivery.RoutingKey == "incidents.status_changed")
                {
                    var message = JsonSerializer.Deserialize<IncidentStatusChangedMessage>(body)
                        ?? throw new JsonException("Incident-status message was empty.");
                    if (_processedMessages.TryAdd(message.MessageId, 0))
                        logger.LogInformation("NOTIFICATION: Incident {IncidentId} status changed {OldStatus} -> {NewStatus}",
                            message.IncidentId, message.OldStatus, message.NewStatus);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported routing key: {delivery.RoutingKey}");
                }

                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception exception)
            {
                var retryCount = ReadRetryCount(delivery.BasicProperties.Headers);
                try
                {
                    var headers = delivery.BasicProperties.Headers is null
                        ? new Dictionary<string, object?>()
                        : new Dictionary<string, object?>(delivery.BasicProperties.Headers);
                    var retryProperties = new BasicProperties
                    {
                        ContentType = delivery.BasicProperties.ContentType,
                        DeliveryMode = DeliveryModes.Persistent,
                        Headers = headers
                    };

                    if (retryCount < _settings.MaxDeliveryAttempts)
                    {
                        headers["x-opsflow-retry-count"] = retryCount + 1;
                        await channel.BasicPublishAsync(_settings.Exchange, delivery.RoutingKey, mandatory: true,
                            basicProperties: retryProperties, body: delivery.Body, cancellationToken: stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        logger.LogWarning(exception, "Message failed; scheduled retry {RetryNumber} of {MaxRetries}.", retryCount + 1, _settings.MaxDeliveryAttempts);
                    }
                    else
                    {
                        await channel.BasicPublishAsync(_settings.DeadLetterExchange, "failed", mandatory: true,
                            basicProperties: retryProperties, body: delivery.Body, cancellationToken: stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        logger.LogError(exception, "Message exceeded {MaxRetries} retries and was sent to the DLQ.", _settings.MaxDeliveryAttempts);
                    }
                }
                catch (Exception publishException)
                {
                    logger.LogError(publishException, "Could not transfer the failed message; asking RabbitMQ to requeue it.");
                    await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
            }
        };

        await channel.BasicConsumeAsync(_settings.Queue, autoAck: false, consumer, cancellationToken: stoppingToken);
        logger.LogInformation("Consuming RabbitMQ queue {Queue}", _settings.Queue);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static int ReadRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-opsflow-retry-count", out var value)) return 0;
        return value switch
        {
            int count => count,
            long count => checked((int)count),
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var count) => count,
            _ when int.TryParse(value?.ToString(), out var count) => count,
            _ => 0
        };
    }
}
