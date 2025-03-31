using RabbitMQ.Client;

namespace Hyperquant.Bybit.Services;

public class RabbitClientHostedService : IHostedService
{
    private IConnection _rabbitConnection;
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private bool _initialized;
    
    private IChannel _channel;
    public IChannel Channel 
    { 
        get
        {
            if (!_initialized)
            {
                InitializeChannelAsync().GetAwaiter().GetResult();
            }
            return _channel;
        }
    }

    public RabbitClientHostedService(IConnection rabbitConnection, IConnectionFactory connectionFactory)
    {
        _rabbitConnection = rabbitConnection;
        _connectionFactory = connectionFactory;
    }

    private async Task InitializeChannelAsync()
    {
        await _initializationSemaphore.WaitAsync();
        try
        {
            if (!_initialized)
            {
                if (_rabbitConnection.IsOpen)
                {
                    _channel = await _rabbitConnection.CreateChannelAsync();
                }
                else
                {
                    _rabbitConnection = await _connectionFactory.CreateConnectionAsync();
                    _channel = await _rabbitConnection.CreateChannelAsync();
                }

                _initialized = true;
            }
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeChannelAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken: cancellationToken);
        }
        
        if (_rabbitConnection is { IsOpen: true })
        {
            await _rabbitConnection.CloseAsync(cancellationToken: cancellationToken);
            await _rabbitConnection.DisposeAsync();
        }
        
        _initializationSemaphore.Dispose();
    }
}