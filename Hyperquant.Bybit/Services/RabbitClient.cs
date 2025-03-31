using System.Text;
using Hyperquant.Abstraction.Rabbit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.Extensions.Logging;

namespace Hyperquant.Bybit.Services;

public class RabbitClient : IRabbitConsumerClient
{
    private readonly RabbitClientHostedService _rabbitClientHostedService;
    private IChannel Channel => _rabbitClientHostedService.Channel;
    private AsyncEventingBasicConsumer _consumer;
    private AsyncEventHandler<BasicDeliverEventArgs> _receivedHandler;
    private readonly ILogger<RabbitClient> _logger;

    public RabbitClient(RabbitClientHostedService rabbitClientHostedService, ILogger<RabbitClient> logger)
    {
        _rabbitClientHostedService = rabbitClientHostedService;
        _logger = logger;
    }

    private void EnsureConsumerInitialized() => 
        _consumer ??= new AsyncEventingBasicConsumer(Channel);

    public async Task PublishAsync<T>(T message, string exchange, string routingKey)
    {
        try
        {
            string json = JsonSerializer.Serialize(message);
            var bytesMessage = Encoding.UTF8.GetBytes(json);

            await Channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, body: bytesMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while publishing message to exchange {Exchange} with routing key {RoutingKey}",
                exchange, routingKey);
            throw;
        }
    }

    public Task ConsumeAsync<T>(string queue, Func<T, Task> callback)
    {
        try
        {
            EnsureConsumerInitialized();
            
            _receivedHandler = async (model, eventArgs) =>
            { 
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    var message = JsonSerializer.Deserialize<T>(json);
                    if (message != null)
                    {
                        await callback(message);
                        await Channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogWarning("Received null message from queue {Queue}", queue);
                        await Channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                        {
                            Error = ex.Message,
                            Queue = queue,
                            DeliveryTag = eventArgs.DeliveryTag,
                            Message = Encoding.UTF8.GetString(eventArgs.Body.ToArray())
                        }));
                        
                        _logger.LogError(ex, "Error while processing message, rejecting and publishing to error queue");
                        await Channel.BasicRejectAsync(eventArgs.DeliveryTag, false);
                        
                        await Channel.BasicPublishAsync("stock_exchange_upload", "error.bybit", body);
                }
            };

            _consumer.ReceivedAsync += _receivedHandler;

            _logger.LogInformation("Consume message from queue {Queue}", queue);
            return Channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: _consumer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while consuming message from queue {Queue}", queue);
            throw;
        }
    }

    public void Dispose()
    {
        if (_consumer != null && _receivedHandler != null)
        {
            _consumer.ReceivedAsync -= _receivedHandler;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumer != null && _receivedHandler != null)
        {
            _consumer.ReceivedAsync -= _receivedHandler;
        }
    }
}