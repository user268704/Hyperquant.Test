using System.Diagnostics;
using Hyperquant.Abstraction.Exchanges;
using Hyperquant.Abstraction.Rabbit;
using Hyperquant.Dto.Dto.UpdateContract;
using OpenTelemetry.Trace;

namespace Hyperquant.Bybit.Services;

public class RabbitConsumerService : IHostedService
{
    private readonly IRabbitConsumerClient _rabbitClient;
    private readonly IExchangeFuturesUpdate _futuresUpdate;
    private readonly ILogger<RabbitConsumerService> _logger;
    
    public const string ActivitySourceName = "Hyperquant.Bybit";
    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    public RabbitConsumerService(
        IRabbitConsumerClient rabbitClient, 
        IExchangeFuturesUpdate futuresUpdate,
        ILogger<RabbitConsumerService> logger)
    {
        _rabbitClient = rabbitClient;
        _futuresUpdate = futuresUpdate;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("Consume start", ActivityKind.Client);

        try
        {
            await _rabbitClient.ConsumeAsync<InitializeUpdateDto>("update.queue", async message =>
            {
                try
                {
                    activity.AddEvent(new ActivityEvent("Start processing message"));
                    var result = await _futuresUpdate.UpdateContract(message);

                    await _rabbitClient.PublishAsync(result, "stock_exchange_upload", "result");
                    activity.AddEvent(new ActivityEvent("Message processed successfully"));
                }
                catch (Exception ex)
                {
                    activity?.RecordException(ex);
                    _logger.LogError(ex, "Error while processing message: {Message}", ex.Message);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while subscribing to queue update.queue. Trying to retry in 5 seconds");
            activity?.RecordException(ex);
            
            await Task.Delay(5000, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                await StartAsync(cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}