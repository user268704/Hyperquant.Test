using RabbitMQ.Client;

namespace Hyperquant.Jobs.Services;

public class RabbitClientHostedService : IHostedService
{
    private IConnection _rabbitConnection;
    private readonly IConnectionFactory _connectionFactory;
    public IChannel Channel { get; private set; }


    public RabbitClientHostedService(IConnection rabbitConnection, IConnectionFactory connectionFactory)
    {
        _rabbitConnection = rabbitConnection;
        _connectionFactory = connectionFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_rabbitConnection.IsOpen)
        {
            Channel = await _rabbitConnection.CreateChannelAsync();
        }
        else
        {
            _rabbitConnection = await _connectionFactory.CreateConnectionAsync();
            Channel = await _rabbitConnection.CreateChannelAsync();
        }
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _rabbitConnection.DisposeAsync();
    }
}