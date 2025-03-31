#region

using System.Text;
using Hyperquant.Abstraction.Rabbit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using JsonSerializer = System.Text.Json.JsonSerializer;

#endregion

namespace Hyperquant.Repository.Services;

public class RabbitClient : IRabbitConsumerClient
{
    private readonly ILogger<RabbitClient> _logger;
    private readonly RabbitClientHostedService _rabbitClientHostedService;
    private AsyncEventingBasicConsumer _consumer;
    private AsyncEventHandler<BasicDeliverEventArgs> _receivedHandler;

    public RabbitClient(RabbitClientHostedService rabbitClientHostedService, ILogger<RabbitClient> logger)
    {
        _rabbitClientHostedService = rabbitClientHostedService;
        _logger = logger;
    }

    private IChannel Channel => _rabbitClientHostedService.Channel;

    public async Task PublishAsync<T>(T message, string exchange, string routingKey)
    {
        try
        {
            string json = JsonSerializer.Serialize(message);
            var bytesMessage = Encoding.UTF8.GetBytes(json);

            await Channel.BasicPublishAsync(exchange, routingKey, bytesMessage);
            _logger.LogInformation("Message published to {Exchange} with routing key {RoutingKey}",
                exchange, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception while publishing message to exchange {Exchange} with routing key {RoutingKey}",
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
                        await callback(message);
                    else
                        _logger.LogWarning("Message deserialization failed for queue {Queue}", queue);
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
                    await Channel.BasicPublishAsync("stock_exchange_upload", "error.repository", body);

                }
            };

            _consumer.ReceivedAsync += _receivedHandler;

            _logger.LogInformation("Consumer initialized for queue {Queue}", queue);
            
            return Channel.BasicConsumeAsync(queue, true, _consumer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Err", queue);
            throw;
        }
    }

    public void Dispose()
    {
        if (_consumer != null && _receivedHandler != null) _consumer.ReceivedAsync -= _receivedHandler;
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumer != null && _receivedHandler != null) _consumer.ReceivedAsync -= _receivedHandler;
    }

    private void EnsureConsumerInitialized()
    {
        if (_consumer == null) _consumer = new AsyncEventingBasicConsumer(Channel);
    }
}