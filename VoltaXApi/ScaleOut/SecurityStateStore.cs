using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace VoltaXApi.ScaleOut;

/// <summary>
/// Short-lived security state shared by every instance (2FA attempt counters and used challenges, one-time tickets).
/// Single-use and counter operations are atomic: Redis GETDEL / INCR / SET NX when Redis is configured,
/// otherwise the in-process <see cref="IDistributedCache"/> under a lock.
/// </summary>
public interface ISecurityStateStore
{
    Task SetAsync(string key, string value, TimeSpan lifetime);

    /// <summary>Returns the value and deletes it in one step: at most one caller ever gets it.</summary>
    Task<string?> TakeAsync(string key);

    Task<bool> ExistsAsync(string key);

    /// <summary>Stores the key only when absent; false when it already exists.</summary>
    Task<bool> TryAddAsync(string key, string value, TimeSpan lifetime);

    /// <summary>Increments a counter (created at 1 with <paramref name="lifetime"/>) and returns the new value.</summary>
    Task<long> IncrementAsync(string key, TimeSpan lifetime);
}

public sealed class DistributedCacheSecurityStateStore : ISecurityStateStore
{
    private const string Prefix = "eveon:sec:";

    // Makes read-modify-write atomic on the in-process cache; with one instance that is enough.
    private static readonly SemaphoreSlim LocalLock = new(1, 1);

    private readonly IDistributedCache _cache;
    private readonly RedisConnection? _redis;

    public DistributedCacheSecurityStateStore(IDistributedCache cache, RedisConnection? redis = null)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task SetAsync(string key, string value, TimeSpan lifetime)
    {
        if (_redis != null)
        {
            await (await Db()).StringSetAsync(Prefix + key, value, lifetime);
            return;
        }
        await _cache.SetStringAsync(Prefix + key, value, Expires(lifetime));
    }

    public async Task<string?> TakeAsync(string key)
    {
        if (_redis != null)
            return await (await Db()).StringGetDeleteAsync(Prefix + key);

        await LocalLock.WaitAsync();
        try
        {
            var value = await _cache.GetStringAsync(Prefix + key);
            if (value != null) await _cache.RemoveAsync(Prefix + key);
            return value;
        }
        finally
        {
            LocalLock.Release();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (_redis != null)
            return await (await Db()).KeyExistsAsync(Prefix + key);
        return await _cache.GetAsync(Prefix + key) != null;
    }

    public async Task<bool> TryAddAsync(string key, string value, TimeSpan lifetime)
    {
        if (_redis != null)
            return await (await Db()).StringSetAsync(Prefix + key, value, lifetime, When.NotExists);

        await LocalLock.WaitAsync();
        try
        {
            if (await _cache.GetAsync(Prefix + key) != null) return false;
            await _cache.SetStringAsync(Prefix + key, value, Expires(lifetime));
            return true;
        }
        finally
        {
            LocalLock.Release();
        }
    }

    public async Task<long> IncrementAsync(string key, TimeSpan lifetime)
    {
        if (_redis != null)
        {
            var db = await Db();
            var count = await db.StringIncrementAsync(Prefix + key);
            if (count == 1) await db.KeyExpireAsync(Prefix + key, lifetime);
            return count;
        }

        await LocalLock.WaitAsync();
        try
        {
            // The window stays the one of the first attempt, as with Redis.
            var stored = await _cache.GetAsync(Prefix + key);
            long current = 0;
            DateTimeOffset expires = DateTimeOffset.UtcNow + lifetime;
            if (stored is { Length: 16 })
            {
                current = BitConverter.ToInt64(stored, 0);
                expires = new DateTimeOffset(BitConverter.ToInt64(stored, 8), TimeSpan.Zero);
            }
            current++;
            var value = BitConverter.GetBytes(current).Concat(BitConverter.GetBytes(expires.UtcTicks)).ToArray();
            await _cache.SetAsync(Prefix + key, value, new DistributedCacheEntryOptions { AbsoluteExpiration = expires });
            return current;
        }
        finally
        {
            LocalLock.Release();
        }
    }

    private async Task<IDatabase> Db() => (await _redis!.GetAsync()).GetDatabase();

    private static DistributedCacheEntryOptions Expires(TimeSpan lifetime) => new() { AbsoluteExpirationRelativeToNow = lifetime };
}
