using System.Collections.Concurrent;
using StackExchange.Redis;

namespace VoltaXApi.ScaleOut;

/// <summary>Fire-and-forget pub/sub between API instances (Redis pub/sub, or in-process without Redis).</summary>
public interface IScaleOutBus
{
    /// <summary>Publishes and returns how many subscribers received the message (0: nobody listens on the channel).</summary>
    Task<long> PublishAsync(string channel, string message);

    /// <summary>Handlers run off the delivering thread; their exceptions are logged by the bus.</summary>
    Task SubscribeAsync(string channel, Func<string, Task> handler);
}

/// <summary>
/// Delivers in-process. With no Redis it is the bus of the single instance; tests share one object
/// between several simulated instances.
/// </summary>
public sealed class InMemoryScaleOutBus : IScaleOutBus
{
    private readonly ConcurrentDictionary<string, ImmutableHandlers> _handlers = new();
    private readonly ILogger<InMemoryScaleOutBus> _logger;

    public InMemoryScaleOutBus(ILogger<InMemoryScaleOutBus> logger)
    {
        _logger = logger;
    }

    public Task<long> PublishAsync(string channel, string message)
    {
        if (!_handlers.TryGetValue(channel, out var handlers))
            return Task.FromResult(0L);
        foreach (var handler in handlers.Items)
            _ = Task.Run(() => ScaleOutBusDispatch.RunAsync(handler, channel, message, _logger));
        return Task.FromResult((long)handlers.Items.Length);
    }

    public Task SubscribeAsync(string channel, Func<string, Task> handler)
    {
        _handlers.AddOrUpdate(channel, _ => new ImmutableHandlers(new[] { handler }),
            (_, existing) => new ImmutableHandlers(existing.Items.Append(handler).ToArray()));
        return Task.CompletedTask;
    }

    private sealed record ImmutableHandlers(Func<string, Task>[] Items);
}

public sealed class RedisScaleOutBus : IScaleOutBus
{
    private readonly RedisConnection _redis;
    private readonly ILogger<RedisScaleOutBus> _logger;

    public RedisScaleOutBus(RedisConnection redis, ILogger<RedisScaleOutBus> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<long> PublishAsync(string channel, string message)
    {
        var multiplexer = await _redis.GetAsync();
        return await multiplexer.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), message);
    }

    public async Task SubscribeAsync(string channel, Func<string, Task> handler)
    {
        var subscriber = (await _redis.GetAsync()).GetSubscriber();
        var redisChannel = RedisChannel.Literal(channel);
        Action<RedisChannel, RedisValue> callback = (from, value) => { _ = Task.Run(() => ScaleOutBusDispatch.RunAsync(handler, channel, value.ToString(), _logger)); };
        try
        {
            await subscriber.SubscribeAsync(redisChannel, callback);
        }
        catch
        {
            // Drops the local registration so a retry does not deliver twice once Redis is back.
            subscriber.Unsubscribe(redisChannel, callback, CommandFlags.FireAndForget);
            throw;
        }
    }
}

internal static class ScaleOutBusDispatch
{
    public static async Task RunAsync(Func<string, Task> handler, string channel, string message, ILogger logger)
    {
        try
        {
            await handler(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scale-out message on {Channel} could not be handled", channel);
        }
    }
}
