using RabbitMQ.Client;

namespace OpsFlow.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public static async Task DeclareAsync(
        IChannel channel,
        RabbitMqSettings settings,
        CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(settings.Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(settings.DeadLetterExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(settings.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(settings.DeadLetterQueue, settings.DeadLetterExchange, "#", cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-delivery-limit"] = (int)settings.MaxDeliveryAttempts,
            ["x-dead-letter-exchange"] = settings.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = "failed"
        };
        await channel.QueueDeclareAsync(settings.Queue, durable: true, exclusive: false, autoDelete: false, arguments: arguments, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(settings.Queue, settings.Exchange, "#", cancellationToken: cancellationToken);
    }
}
