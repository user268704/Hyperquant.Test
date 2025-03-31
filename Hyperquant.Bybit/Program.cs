using CryptoExchange.Net.Authentication;
using Hyperquant.Abstraction.Exchanges;
using Hyperquant.Abstraction.Rabbit;
using Hyperquant.Aspire.ServiceDefaults;
using Hyperquant.Bybit.Services;
using Serilog;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.AddRabbitMQClient("bus");
builder.AddServiceDefaults();

builder.Services.AddSingleton<RabbitClientHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitClientHostedService>());

builder.Services.AddSingleton<IRabbitConsumerClient, RabbitClient>();
builder.Services.AddSingleton<IRabbitClient>(sp => 
    (RabbitClient)sp.GetRequiredService<IRabbitConsumerClient>());

builder.Services.AddSingleton<IExchangeFuturesUpdate, BybitFuturesUpdate>();
builder.Services.AddHostedService<BybitFuturesUpdate>(sp => 
    (BybitFuturesUpdate)sp.GetRequiredService<IExchangeFuturesUpdate>());

builder.Services.AddHostedService<RabbitConsumerService>();

ILogger loggerConfiguration = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.WithProperty("app", "Hyperquant.Bybit")
    .CreateLogger();

string apiKey = builder.Configuration["BYBIT_API_KEY"];
string apiSecret = builder.Configuration["BYBIT_API_SECRET"];

if (string.IsNullOrEmpty(apiKey))
{
    loggerConfiguration.Fatal("BYBIT_API_KEY is not set. Please set it in the configuration.");
    
    return;
}

builder.Services.AddBybit(options =>
{
    options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

app.Run();