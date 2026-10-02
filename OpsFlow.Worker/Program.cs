using OpsFlow.Infrastructure.Messaging;
using OpsFlow.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<IncidentNotificationWorker>();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

var host = builder.Build();
host.Run();
