#region

using RabbitMQ.Client;

#endregion

namespace Hyperquant.Repository.Services;

public class RabbitClientHostedService : IHostedService
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

    private IChannel _channel;
    private bool _initialized;
    private IConnection _rabbitConnection;

    public RabbitClientHostedService(IConnection rabbitConnection, IConnectionFactory connectionFactory)
    {
        _rabbitConnection = rabbitConnection;
        _connectionFactory = connectionFactory;
    }

    public IChannel Channel
    {
        get
        {
            if (!_initialized) InitializeChannelAsync().GetAwaiter().GetResult();
            return _channel;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeChannelAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync();

        if (_rabbitConnection != null && _rabbitConnection.IsOpen)
        {
            await _rabbitConnection.CloseAsync();
            await _rabbitConnection.DisposeAsync();
        }

        _initializationSemaphore.Dispose();
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


                DeclareQueue();
            }
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    private void DeclareQueue()
    {
        Channel.QueueDeclareAsync("result.queue", true, false, false,
            null);

        Channel.QueueBindAsync("result.queue", "stock_exchange_upload", "result");
    }
}