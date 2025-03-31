using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HyperquanTest.ApiGateway.Services;

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
            Channel = await _rabbitConnection.CreateChannelAsync(cancellationToken: cancellationToken);
        }
        else
        {
            _rabbitConnection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            Channel = await _rabbitConnection.CreateChannelAsync(cancellationToken: cancellationToken);
        }
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _rabbitConnection.DisposeAsync();
    }
}