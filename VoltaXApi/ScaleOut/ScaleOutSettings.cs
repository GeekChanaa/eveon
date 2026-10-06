namespace VoltaXApi.ScaleOut;

/// <summary>
/// Scale-out configuration (section "ScaleOut"). An empty ScaleOut:Redis:ConnectionString means a single
/// instance with in-memory state; with Redis, N instances share charger ownership, commands and SignalR events.
/// </summary>
public sealed class ScaleOutSettings
{
    public const string ChannelPrefix = "eveon";

    public ScaleOutSettings(string instanceId, string? redisConnectionString, TimeSpan heartbeat, TimeSpan ownershipTtl)
    {
        InstanceId = instanceId;
        RedisConnectionString = string.IsNullOrWhiteSpace(redisConnectionString) ? null : redisConnectionString;
        Heartbeat = heartbeat;
        OwnershipTtl = ownershipTtl;
    }

    /// <summary>Unique per running process; chargers connected here are owned by this id.</summary>
    public string InstanceId { get; }

    public string? RedisConnectionString { get; }

    public bool UsesRedis => RedisConnectionString != null;

    /// <summary>How often ownership entries of the local chargers are refreshed.</summary>
    public TimeSpan Heartbeat { get; }

    /// <summary>Ownership entries expire when the owning instance stops refreshing them.</summary>
    public TimeSpan OwnershipTtl { get; }

    public static ScaleOutSettings FromConfiguration(IConfiguration configuration)
    {
        var heartbeat = Math.Max(1, configuration.GetValue("ScaleOut:HeartbeatSeconds", 30));
        var ttl = Math.Max(heartbeat * 2, configuration.GetValue("ScaleOut:OwnershipTtlSeconds", 90));
        var instanceId = configuration["ScaleOut:InstanceId"];
        if (string.IsNullOrWhiteSpace(instanceId))
            instanceId = $"{Environment.MachineName}-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..6]}";
        return new ScaleOutSettings(instanceId, configuration["ScaleOut:Redis:ConnectionString"],
            TimeSpan.FromSeconds(heartbeat), TimeSpan.FromSeconds(ttl));
    }

    public static string CommandChannel(string instanceId) => $"{ChannelPrefix}:ocpp:cmd:{instanceId}";
    public static string ReplyChannel(string instanceId) => $"{ChannelPrefix}:ocpp:reply:{instanceId}";
    public static string CloseStaleChannel(string instanceId) => $"{ChannelPrefix}:ocpp:close-stale:{instanceId}";
    public const string HubRevokeChannel = ChannelPrefix + ":hub:revoke";
}
