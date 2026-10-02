namespace OpsFlow.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(string routingKey, TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
