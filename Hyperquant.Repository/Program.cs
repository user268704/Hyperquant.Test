#region

using Hyperquant.Abstraction.Rabbit;
using Hyperquant.Abstraction.Repository;
using Hyperquant.Aspire.ServiceDefaults;
using Hyperquant.Data.Contexts;
using Hyperquant.Repository.Services;

#endregion

var builder = WebApplication.CreateBuilder(args);


builder.AddRabbitMQClient("bus");

builder.AddNpgsqlDbContext<PostgresContext>("hyperquant");
builder.AddServiceDefaults();

builder.Services.AddScoped<IUpdateFuturesRepository, UpdateFuturesRepository>();

builder.Services.AddSingleton<RabbitClientHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitClientHostedService>());

builder.Services.AddSingleton<IRabbitConsumerClient, RabbitClient>();
builder.Services.AddSingleton<IRabbitClient>(sp =>
    (RabbitClient)sp.GetRequiredService<IRabbitConsumerClient>());

builder.Services.AddHostedService<RabbitConsumerService>();


var app = builder.Build();

app.Run();