using System.Collections.Concurrent;
using System.Net.WebSockets;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  /// <summary>Registry of the live charger connections, one per charge point id.</summary>
  public class WebSocketManagerService
  {
    private readonly ConcurrentDictionary<string, OcppConnection> _connections = new();

    /// <summary>Registers the connection and returns the one it replaces (a charger that reconnected), if any.</summary>
    public OcppConnection? AddConnection(OcppConnection connection)
    {
      OcppConnection? previous = null;
      _connections.AddOrUpdate(connection.ChargePointId, connection, (_, existing) => { previous = existing; return connection; });
      return previous;
    }

    /// <summary>Removes the connection unless a newer one for the same charge point already took its place.</summary>
    public bool RemoveConnection(OcppConnection connection) =>
      _connections.TryRemove(new KeyValuePair<string, OcppConnection>(connection.ChargePointId, connection));

    public OcppConnection? GetConnection(string chargePointId) =>
      _connections.TryGetValue(chargePointId, out var connection) ? connection : null;

    /// <summary>Snapshot of this instance's connections.</summary>
    public IReadOnlyCollection<OcppConnection> GetConnections() => _connections.Values.ToArray();

    public WebSocket? GetWebSocket(string chargePointId) => GetConnection(chargePointId)?.WebSocket;

    public bool GetWebSocketStatus(string chargePointID) => GetConnection(chargePointID)?.IsOpen == true;

    /// <summary>Sends raw text through the connection's send lock.</summary>
    public async Task SendMessageAsync(string chargePointId, string message, CancellationToken cancellationToken = default)
    {
      var connection = GetConnection(chargePointId)
        ?? throw new WebSocketNotFoundException($"Charge point {chargePointId} is not connected.");
      await connection.SendTextAsync(message, cancellationToken);
    }
  }
}
