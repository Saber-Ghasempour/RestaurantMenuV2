using RabbitMQ.Client;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public interface IRabbitMqConnection : IAsyncDisposable
{
    Task<IChannel> CreateChannelAsync(bool publisherConfirms, CancellationToken cancellationToken);
    Task<bool> ProbeAsync(CancellationToken cancellationToken);
}

public sealed class RabbitMqConnection(MessagingOptions options) : IRabbitMqConnection
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IChannel> CreateChannelAsync(bool publisherConfirms,
        CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(publisherConfirmationsEnabled: publisherConfirms,
            publisherConfirmationTrackingEnabled: publisherConfirms);
        return await connection.CreateChannelAsync(channelOptions, cancellationToken);
    }

    public async Task<bool> ProbeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken);
            return connection.IsOpen;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        { return false; }
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true }) return _connection;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            if (_connection is not null) await _connection.DisposeAsync();
            var factory = new ConnectionFactory
            {
                Uri = new Uri(options.ConnectionString),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName = "restaurant-menu"
            };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
