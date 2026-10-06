using StackExchange.Redis;

namespace VoltaXApi.ScaleOut;

/// <summary>
/// The one Redis multiplexer of the process, shared by the SignalR backplane, the distributed cache and the
/// scale-out services. Connects lazily and never fails start-up: while Redis is down the multiplexer keeps
/// reconnecting in the background and Redis operations throw <see cref="RedisConnectionException"/>.
/// </summary>
public sealed class RedisConnection : IAsyncDisposable
{
    private readonly Lazy<Task<IConnectionMultiplexer>> _multiplexer;

    public RedisConnection(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        // While Redis is down, operations fail at once instead of queueing until the timeout.
        options.BacklogPolicy = BacklogPolicy.FailFast;
        options.ClientName ??= "eveon-api";
        _multiplexer = new Lazy<Task<IConnectionMultiplexer>>(async () => await ConnectionMultiplexer.ConnectAsync(options));
    }

    public Task<IConnectionMultiplexer> GetAsync() => _multiplexer.Value;

    /// <summary>For synchronous callers; only blocks while the first connection attempt runs.</summary>
    public IConnectionMultiplexer Get() => _multiplexer.Value.GetAwaiter().GetResult();

    public bool IsConnected => _multiplexer.IsValueCreated && _multiplexer.Value.IsCompletedSuccessfully && _multiplexer.Value.Result.IsConnected;

    public async ValueTask DisposeAsync()
    {
        if (_multiplexer.IsValueCreated && _multiplexer.Value.IsCompletedSuccessfully)
            await _multiplexer.Value.Result.DisposeAsync();
    }
}
