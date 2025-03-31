using System.Diagnostics;
using OpenTelemetry.Trace;
using RabbitMQ.Client;

namespace Hyperquant.RabbitMigrator;

public class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private ActivitySource _activity = new(ActivitySourceName);
    public const string ActivitySourceName = "RabbitMigrator";

    public Worker(IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider, ILogger<Worker> logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = _activity.StartActivity("Migrating RabbitMQ", ActivityKind.Client);

        try
        {
            _logger.LogInformation("Start migration for RabbitMQ");
            
            var connection = _serviceProvider.GetRequiredService<IConnection>();
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await CreateExchanges(channel);
            await CreateQueues(channel);
            await BindQueues(channel);
            
            _logger.LogInformation("Migration completed for RabbitMQ");
        }
        catch (Exception e)
        {
            activity?.RecordException(e);
            throw;
        }
        
        _hostApplicationLifetime.StopApplication();
    }

    private async Task BindQueues(IChannel channel)
    {
        await channel.QueueBindAsync("update.queue", "stock_exchange_upload", "update");
        await channel.QueueBindAsync("result.queue", "stock_exchange_upload", "result");
        await channel.QueueBindAsync("update.error.queue", "stock_exchange_upload", "error.*");
    }

    private async Task CreateExchanges(IChannel channel)
    {
        await channel.ExchangeDeclareAsync("stock_exchange_upload", ExchangeType.Topic, 
            durable: true,
            autoDelete: false,
            arguments: null);
    }

    private async Task CreateQueues(IChannel channel)
    {
        await channel.QueueDeclareAsync("update.queue", durable: true, exclusive: false, autoDelete: false,
            arguments: null);

        await channel.QueueDeclareAsync("result.queue", durable: true, exclusive: false, autoDelete: false,
            arguments: null);

        await channel.QueueDeclareAsync("update.error.queue", durable: true, exclusive: false, autoDelete: false,
            arguments: null);
    }
}