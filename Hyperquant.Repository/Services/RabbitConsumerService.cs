#region

using System.Diagnostics;
using System.Text.Json;
using Hyperquant.Abstraction.Rabbit;
using Hyperquant.Abstraction.Repository;
using Hyperquant.Dto.Dto.UpdateContract;
using OpenTelemetry.Trace;

#endregion

namespace Hyperquant.Repository.Services;

public class RabbitConsumerService : IHostedService
{
    private readonly ILogger<RabbitConsumerService> _logger;
    private IUpdateFuturesRepository _updateFuturesRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitConsumerClient _rabbitClient;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;
    
    private const string ActivitySourceName = "Hyperquant.Repository";
    private ActivitySource _activitySource = new(ActivitySourceName);

    public RabbitConsumerService(
        IServiceProvider serviceProvider,
        IRabbitConsumerClient rabbitClient,
        ILogger<RabbitConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitClient = rabbitClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("Consume start", ActivityKind.Consumer);
        
        var scoped = _serviceProvider.CreateScope();
        _updateFuturesRepository = scoped.ServiceProvider.GetRequiredService<IUpdateFuturesRepository>();

        await _rabbitClient.ConsumeAsync<UpdateContractResult>("result.queue", async message =>
        {
            var retryCount = 0;
            while (retryCount < MaxRetries)
            {
                try
                {
                    activity?.AddEvent(new ActivityEvent("Start processing message"));
                    await _updateFuturesRepository.UpdateFuturesAsync(message);
                    activity?.AddEvent(new ActivityEvent("Message processed successfully"));
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    activity?.RecordException(ex);
                    _logger.LogError(ex, "Error while processing message (attempt {RetryCount}/{MaxRetries}): {Message}", 
                        retryCount, MaxRetries, ex.Message);
                    
                    if (retryCount == MaxRetries)
                    {
                        _logger.LogError("Max retries reached for message. Moving to error queue.");
                        await _rabbitClient.PublishAsync(message, "stock_exchange_upload", "error.repository");
                        throw;
                    }
                    
                    await Task.Delay(RetryDelayMs * retryCount, cancellationToken);
                }
                finally
                {
                    activity?.Stop();
                }
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}