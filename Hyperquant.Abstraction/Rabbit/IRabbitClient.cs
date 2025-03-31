namespace Hyperquant.Abstraction.Rabbit;

public interface IRabbitClient : IDisposable, IAsyncDisposable
{
    public Task PublishAsync<T>(T message, string exchange, string routingKey);
}