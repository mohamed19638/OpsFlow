using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using OpsFlow.Application.Interfaces;

namespace OpsFlow.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqPublisher(IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;
        _connectionFactory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            AutomaticRecoveryEnabled = true
        };
    }

    public async Task PublishAsync<TMessage>(string routingKey, TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
        await RabbitMqTopology.DeclareAsync(channel, _settings, cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
        await channel.BasicPublishAsync(_settings.Exchange, routingKey, mandatory: true, basicProperties: properties, body: body, cancellationToken: cancellationToken);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true }) return _connection;
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not { IsOpen: true })
            {
                if (_connection is not null) await _connection.DisposeAsync();
                _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            }
            return _connection;
        }
        finally { _connectionLock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}
