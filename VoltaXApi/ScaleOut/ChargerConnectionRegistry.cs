using System.Collections.Concurrent;
using System.Text.Json;
using StackExchange.Redis;

namespace VoltaXApi.ScaleOut;

/// <summary>Which API instance holds a charger's WebSocket.</summary>
public sealed record ChargerOwnership(string InstanceId, string ProtocolVersion, DateTimeOffset ConnectedAt, DateTimeOffset LastSeen);

/// <summary>
/// Cluster-wide map chargePointId -> owning instance. Entries expire after <see cref="ScaleOutSettings.OwnershipTtl"/>
/// unless the owner refreshes them, so a crashed instance releases its chargers on its own.
/// </summary>
public interface IChargerConnectionRegistry
{
    /// <summary>Records <paramref name="ownership"/> and returns the entry it replaced, if any.</summary>
    Task<ChargerOwnership?> ClaimAsync(string chargePointId, ChargerOwnership ownership);

    /// <summary>Extends the entry; false when another instance owns the charger (the local socket is stale).</summary>
    Task<bool> RefreshAsync(string chargePointId, ChargerOwnership ownership);

    /// <summary>Removes the entry if <paramref name="instanceId"/> still owns it; false when another instance took it over.</summary>
    Task<bool> ReleaseAsync(string chargePointId, string instanceId);

    Task<ChargerOwnership?> GetAsync(string chargePointId);

    ChargerOwnership? Get(string chargePointId);
}

public sealed class InMemoryChargerConnectionRegistry : IChargerConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ChargerOwnership> _entries = new();
    private readonly TimeSpan _ttl;

    public InMemoryChargerConnectionRegistry(ScaleOutSettings settings) : this(settings.OwnershipTtl) { }

    public InMemoryChargerConnectionRegistry(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    public Task<ChargerOwnership?> ClaimAsync(string chargePointId, ChargerOwnership ownership)
    {
        ChargerOwnership? previous = null;
        _entries.AddOrUpdate(chargePointId, ownership, (_, existing) => { previous = Live(existing); return ownership; });
        return Task.FromResult(previous);
    }

    public Task<bool> RefreshAsync(string chargePointId, ChargerOwnership ownership)
    {
        var refreshed = true;
        _entries.AddOrUpdate(chargePointId, ownership, (_, existing) =>
        {
            if (Live(existing) is { } live && live.InstanceId != ownership.InstanceId) { refreshed = false; return existing; }
            return ownership;
        });
        return Task.FromResult(refreshed);
    }

    public Task<bool> ReleaseAsync(string chargePointId, string instanceId)
    {
        while (_entries.TryGetValue(chargePointId, out var existing))
        {
            if (Live(existing) is { } live && live.InstanceId != instanceId)
                return Task.FromResult(false);
            if (_entries.TryRemove(new KeyValuePair<string, ChargerOwnership>(chargePointId, existing)))
                return Task.FromResult(true);
        }
        return Task.FromResult(true);
    }

    public Task<ChargerOwnership?> GetAsync(string chargePointId) => Task.FromResult(Get(chargePointId));

    public ChargerOwnership? Get(string chargePointId) => _entries.TryGetValue(chargePointId, out var entry) ? Live(entry) : null;

    private ChargerOwnership? Live(ChargerOwnership entry) => entry.LastSeen + _ttl > DateTimeOffset.UtcNow ? entry : null;
}

/// <summary>One string key per charger ("eveon:ocpp:owner:{id}") holding JSON, with a Redis TTL; compare-and-set in Lua.</summary>
public sealed class RedisChargerConnectionRegistry : IChargerConnectionRegistry
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private const string ClaimScript = """
        local old = redis.call('GET', KEYS[1])
        redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2])
        return old
        """;

    private const string RefreshScript = """
        local old = redis.call('GET', KEYS[1])
        if old and cjson.decode(old).instanceId ~= ARGV[1] then return 0 end
        redis.call('SET', KEYS[1], ARGV[2], 'PX', ARGV[3])
        return 1
        """;

    private const string ReleaseScript = """
        local old = redis.call('GET', KEYS[1])
        if not old then return 1 end
        if cjson.decode(old).instanceId ~= ARGV[1] then return 0 end
        redis.call('DEL', KEYS[1])
        return 1
        """;

    private readonly RedisConnection _redis;
    private readonly TimeSpan _ttl;

    public RedisChargerConnectionRegistry(RedisConnection redis, ScaleOutSettings settings)
    {
        _redis = redis;
        _ttl = settings.OwnershipTtl;
    }

    public async Task<ChargerOwnership?> ClaimAsync(string chargePointId, ChargerOwnership ownership)
    {
        var db = (await _redis.GetAsync()).GetDatabase();
        var old = await db.ScriptEvaluateAsync(ClaimScript, new RedisKey[] { Key(chargePointId) },
            new RedisValue[] { Serialize(ownership), (long)_ttl.TotalMilliseconds });
        return old.IsNull ? null : Deserialize((string?)old);
    }

    public async Task<bool> RefreshAsync(string chargePointId, ChargerOwnership ownership)
    {
        var db = (await _redis.GetAsync()).GetDatabase();
        var result = await db.ScriptEvaluateAsync(RefreshScript, new RedisKey[] { Key(chargePointId) },
            new RedisValue[] { ownership.InstanceId, Serialize(ownership), (long)_ttl.TotalMilliseconds });
        return (long)result == 1;
    }

    public async Task<bool> ReleaseAsync(string chargePointId, string instanceId)
    {
        var db = (await _redis.GetAsync()).GetDatabase();
        var result = await db.ScriptEvaluateAsync(ReleaseScript, new RedisKey[] { Key(chargePointId) }, new RedisValue[] { instanceId });
        return (long)result == 1;
    }

    public async Task<ChargerOwnership?> GetAsync(string chargePointId)
    {
        var value = await (await _redis.GetAsync()).GetDatabase().StringGetAsync(Key(chargePointId));
        return value.IsNull ? null : Deserialize(value);
    }

    public ChargerOwnership? Get(string chargePointId)
    {
        var value = _redis.Get().GetDatabase().StringGet(Key(chargePointId));
        return value.IsNull ? null : Deserialize(value);
    }

    private static RedisKey Key(string chargePointId) => $"{ScaleOutSettings.ChannelPrefix}:ocpp:owner:{chargePointId}";

    private static string Serialize(ChargerOwnership ownership) => JsonSerializer.Serialize(ownership, Json);

    private static ChargerOwnership? Deserialize(string? json) => json == null ? null : JsonSerializer.Deserialize<ChargerOwnership>(json, Json);
}
