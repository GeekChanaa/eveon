using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using VoltaXApi.ScaleOut;
namespace VoltaXApi.Authorization;

/// <summary>
/// SignalR connections of this instance by user, so access changes can abort them. <see cref="Revoke"/> also
/// broadcasts to the other instances (scale-out bus); without a bus only local connections are revoked.
/// </summary>
public sealed class HubConnections
{
    private sealed record RevocationMessage(string Origin, int[] UserIds);

    private readonly ConcurrentDictionary<string, (int UserId, HubCallerContext Context)> connections = new();
    private readonly IScaleOutBus? bus;
    private readonly string instanceId;
    private readonly ILogger<HubConnections>? logger;

    public HubConnections(IScaleOutBus? bus = null, ScaleOutSettings? settings = null, ILogger<HubConnections>? logger = null)
    {
        this.bus = bus;
        this.logger = logger;
        instanceId = settings?.InstanceId ?? Guid.NewGuid().ToString("N");
    }

    public void Add(int userId, HubCallerContext context) => connections[context.ConnectionId] = (userId, context);
    public void Remove(string connectionId) => connections.TryRemove(connectionId, out _);

    public void Revoke(IEnumerable<int> userIds)
    {
        var ids = userIds.ToArray();
        RevokeLocal(ids);
        if (bus != null && ids.Length > 0)
            _ = BroadcastAsync(ids);
    }

    /// <summary>Handles a revocation published by another instance.</summary>
    public void HandleRemoteRevocation(string message)
    {
        var revocation = JsonSerializer.Deserialize<RevocationMessage>(message, RemoteOcppCommandClient.Json);
        if (revocation == null || revocation.Origin == instanceId) return;
        RevokeLocal(revocation.UserIds);
    }

    private void RevokeLocal(IEnumerable<int> userIds)
    {
        var ids = userIds.ToHashSet();
        foreach (var pair in connections.Where(pair => ids.Contains(pair.Value.UserId)))
        { pair.Value.Context.Abort(); connections.TryRemove(pair.Key, out _); }
    }

    private async Task BroadcastAsync(int[] userIds)
    {
        try
        {
            await bus!.PublishAsync(ScaleOutSettings.HubRevokeChannel, JsonSerializer.Serialize(new RevocationMessage(instanceId, userIds), RemoteOcppCommandClient.Json));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Could not broadcast the revocation of the SignalR connections of {Count} user(s) to the other instances", userIds.Length);
        }
    }
}
