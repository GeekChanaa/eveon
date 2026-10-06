using System.Text.Json;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.ScaleOut;

/// <summary>
/// Keeps <see cref="IChargerConnectionRegistry"/> in sync with the chargers connected to this instance.
/// Registry failures never break a charger connection: the instance keeps serving its local chargers.
/// </summary>
public sealed class ChargerOwnershipTracker
{
    private sealed record CloseStaleMessage(string ChargePointId, string NewInstanceId, DateTimeOffset ConnectedAt);

    private readonly IChargerConnectionRegistry _registry;
    private readonly IScaleOutBus _bus;
    private readonly WebSocketManagerService _connections;
    private readonly ScaleOutSettings _settings;
    private readonly ILogger<ChargerOwnershipTracker> _logger;

    public ChargerOwnershipTracker(IChargerConnectionRegistry registry, IScaleOutBus bus, WebSocketManagerService connections,
        ScaleOutSettings settings, ILogger<ChargerOwnershipTracker> logger)
    {
        _registry = registry;
        _bus = bus;
        _connections = connections;
        _settings = settings;
        _logger = logger;
    }

    public string InstanceId => _settings.InstanceId;

    /// <summary>Claims the charger; a previous owner on another instance is told to close its stale socket.</summary>
    public async Task OnConnectedAsync(OcppConnection connection)
    {
        try
        {
            var previous = await _registry.ClaimAsync(connection.ChargePointId, Ownership(connection));
            if (previous == null || previous.InstanceId == _settings.InstanceId)
                return;

            _logger.LogInformation("Charge point {ChargePointId} moved from instance {PreviousInstance}: closing its stale connection there",
                connection.ChargePointId, previous.InstanceId);
            var message = new CloseStaleMessage(connection.ChargePointId, _settings.InstanceId, connection.ConnectedAt);
            await _bus.PublishAsync(ScaleOutSettings.CloseStaleChannel(previous.InstanceId), JsonSerializer.Serialize(message, RemoteOcppCommandClient.Json));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not record that {ChargePointId} is connected to instance {InstanceId}", connection.ChargePointId, _settings.InstanceId);
        }
    }

    /// <summary>Releases the charger; false when another instance owns it now (it is not offline).</summary>
    public async Task<bool> OnDisconnectedAsync(OcppConnection connection)
    {
        try
        {
            return await _registry.ReleaseAsync(connection.ChargePointId, _settings.InstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not release the ownership of {ChargePointId}; treating it as disconnected", connection.ChargePointId);
            return true;
        }
    }

    /// <summary>Handles a close-stale message addressed to this instance.</summary>
    public void HandleCloseStale(string message)
    {
        var request = JsonSerializer.Deserialize<CloseStaleMessage>(message, RemoteOcppCommandClient.Json);
        if (request == null) return;
        var connection = _connections.GetConnection(request.ChargePointId);
        // A connection newer than the takeover is the charger coming back here, not the stale one.
        if (connection == null || connection.ConnectedAt > request.ConnectedAt) return;
        _logger.LogInformation("Charge point {ChargePointId} reconnected to instance {InstanceId}: closing the stale local connection",
            request.ChargePointId, request.NewInstanceId);
        connection.Abort();
    }

    /// <summary>Extends the entries of every local charger; a socket that lost its ownership is stale and closed.</summary>
    public async Task HeartbeatAsync()
    {
        foreach (var connection in _connections.GetConnections())
        {
            if (!connection.IsOpen) continue;
            try
            {
                if (!await _registry.RefreshAsync(connection.ChargePointId, Ownership(connection)))
                {
                    _logger.LogInformation("Charge point {ChargePointId} is owned by another instance: closing the stale local connection", connection.ChargePointId);
                    connection.Abort();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not refresh the ownership of {ChargePointId}", connection.ChargePointId);
                return;
            }
        }
    }

    private ChargerOwnership Ownership(OcppConnection connection) =>
        new(_settings.InstanceId, connection.ProtocolVersion, connection.ConnectedAt, DateTimeOffset.UtcNow);
}
