using Hyperquant.Abstraction;
using Hyperquant.Abstraction.Rabbit;
using Hyperquant.Aspire.ServiceDefaults;
using Hyperquant.Dto.Dto.LogContracts;
using Hyperquant.Dto.Dto.RestResponses;
using Hyperquant.Dto.Dto.UpdateContract;
using HyperquanTest.ApiGateway.Services;
using RabbitMQ.Client;
using Serilog;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("bus");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Hyperquant Gateway API", Version = "v1" });
});

ILogger loggerConfiguration = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger = loggerConfiguration;
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration);

builder.Services.AddSingleton<RabbitClientHostedService>();
builder.Services.AddHostedService<RabbitClientHostedService>(x => x.GetRequiredService<RabbitClientHostedService>());
builder.Services.AddSingleton<IChannel>(x =>
{
    var clientHostedService = x.GetRequiredService<RabbitClientHostedService>();

    return clientHostedService.Channel;
});

builder.Services.AddSingleton<IRabbitClient, RabbitClient>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hyperquant Gateway API V1");
        c.RoutePrefix = "swagger";
    });
    app.MapOpenApi();
}

app.MapPost("/start-update", async (HttpContext context, InitializeUpdateDto initialize) =>
{
    var rabbitClient = context.RequestServices.GetRequiredService<IRabbitClient>();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    if (initialize == null)
    {
        logger.LogInformation("{@Error}", new ErrorLog
        {
            Message = "Invalid request: initialize parameter is missing.",
            TimeStamp = DateTime.Now,
            StackTrace = ""
        });

        return Results.BadRequest(new ErrorResponse
        {
            Message = "'initialize' parameter is missing.",
            Code = 400
        });
    }

    // Validate input data
    if (string.IsNullOrEmpty(initialize.FuturesFirst) || string.IsNullOrEmpty(initialize.FuturesSecond))
    {
        logger.LogInformation("{@Error}", new ErrorLog
        {
            Message = "Invalid request: futures names cannot be empty.",
            TimeStamp = DateTime.Now,
            StackTrace = ""
        });

        return Results.BadRequest(new ErrorResponse
        {
            Message = "Futures names cannot be empty.",
            Code = 400
        });
    }

    if (initialize.From >= initialize.To)
    {
        logger.LogInformation("{@Error}", new ErrorLog
        {
            Message = "Invalid request: From date must be less than To date.",
            TimeStamp = DateTime.Now,
            StackTrace = ""
        });

        return Results.BadRequest(new ErrorResponse
        {
            Message = "From date must be less than To date.",
            Code = 400
        });
    }

    if (initialize.From < DateTime.Now.AddDays(-30))
    {
        logger.LogInformation("{@Error}", new ErrorLog
        {
            Message = "Invalid request: From date cannot be more than 30 days ago.",
            TimeStamp = DateTime.Now,
            StackTrace = ""
        });

        return Results.BadRequest(new ErrorResponse
        {
            Message = "From date cannot be more than 30 days ago.",
            Code = 400
        });
    }
    
    logger.LogInformation("Starting update with parameters: {@Initialize}", initialize);
    await rabbitClient.PublishAsync(initialize, "stock_exchange_upload", "update");

    logger.LogInformation("{@Info} {User}", new InfoLog
    {
        Message = "Update started successfully.",
        Timestamp = DateTime.Now,
    }, context.User.Identity?.Name);

    return Results.Ok(new OkResponse
    {
        Message = "Update started successfully."
    });
});

app.Run();
