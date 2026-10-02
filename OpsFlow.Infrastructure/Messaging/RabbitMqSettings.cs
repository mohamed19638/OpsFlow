namespace OpsFlow.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "opsflow.events";
    public string Queue { get; set; } = "opsflow.notifications";
    public string DeadLetterExchange { get; set; } = "opsflow.events.dlx";
    public string DeadLetterQueue { get; set; } = "opsflow.notifications.dlq";
    public ushort MaxDeliveryAttempts { get; set; } = 3;
}
