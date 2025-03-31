using Hyperquant.Aspire.ServiceDefaults;
using Hyperquant.RabbitMigrator;

var builder = Host.CreateApplicationBuilder(args);

builder.AddRabbitMQClient("bus");

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();