using System.Text;
using Hyperquant.Abstraction;
using Hyperquant.Abstraction.Rabbit;
using RabbitMQ.Client;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HyperquanTest.ApiGateway.Services;

public class RabbitClient : IRabbitClient
{
    private readonly IChannel _channel;

    public RabbitClient(IChannel channel)
    {
        _channel = channel;
    }

    public async Task PublishAsync<T>(T message, string exchange, string routingKey)
    {
        string json = JsonSerializer.Serialize(message);
        var bytesMessage = Encoding.UTF8.GetBytes(json);
        
        await _channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, body: bytesMessage);
    }

    public void Dispose()
    {
        if (_channel != null) _channel.Dispose();

    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
    }
}