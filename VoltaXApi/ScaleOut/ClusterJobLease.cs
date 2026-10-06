using StackExchange.Redis;

namespace VoltaXApi.ScaleOut;

/// <summary>
/// Lets one replica run a periodic background job (retention, GDPR, cost updates, OCPI push...).
/// The holder keeps the lease by acquiring it again on every run; when it stops, another
/// replica takes over after the TTL. Without Redis there is only one instance, so it always wins.
/// </summary>
public interface IClusterJobLease
{
    /// <summary>True when this instance may run <paramref name="jobName"/> now. Pick a TTL longer than the job's interval.</summary>
    Task<bool> TryAcquireAsync(string jobName, TimeSpan ttl, CancellationToken cancellationToken = default);
}

public sealed class InProcessClusterJobLease : IClusterJobLease
{
    public Task<bool> TryAcquireAsync(string jobName, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

public sealed class RedisClusterJobLease : IClusterJobLease
{
    // Take the lease if free, or extend it if we already hold it; atomic so two replicas never both win.
    private const string AcquireScript = @"
local owner = redis.call('GET', KEYS[1])
if not owner then
  redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2])
  return 1
end
if owner == ARGV[1] then
  redis.call('PEXPIRE', KEYS[1], ARGV[2])
  return 1
end
return 0";

    private readonly RedisConnection _redis;
    private readonly ScaleOutSettings _settings;
    private readonly ILogger<RedisClusterJobLease> _logger;

    public RedisClusterJobLease(RedisConnection redis, ScaleOutSettings settings, ILogger<RedisClusterJobLease> logger)
    {
        _redis = redis;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(string jobName, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = (await _redis.GetAsync()).GetDatabase();
            var result = await db.ScriptEvaluateAsync(AcquireScript,
                new RedisKey[] { $"{ScaleOutSettings.ChannelPrefix}:job:{jobName}" },
                new RedisValue[] { _settings.InstanceId, (long)ttl.TotalMilliseconds });
            return (long)result == 1;
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or ObjectDisposedException)
        {
            // Skipping one run is safe; running the same job on every replica is not.
            _logger.LogWarning(ex, "Could not acquire the cluster lease for {Job}; skipping this run", jobName);
            return false;
        }
    }
}
