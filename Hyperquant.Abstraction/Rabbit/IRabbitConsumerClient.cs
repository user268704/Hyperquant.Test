namespace Hyperquant.Abstraction.Rabbit;

public interface IRabbitConsumerClient : IRabbitClient
{
    public Task ConsumeAsync<T>(string queue, Func<T, Task> callback);
}