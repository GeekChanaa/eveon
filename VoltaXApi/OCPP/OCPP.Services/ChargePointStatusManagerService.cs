using OCPP.Core.Server;
using VoltaXApi.ScaleOut;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Read-only view of the connected charge points: the live connections of <see cref="WebSocketManagerService"/>,
    /// then the charger registry for chargers connected to another instance.
    /// </summary>
    public class ChargePointStatusManagerService
    {
        private readonly WebSocketManagerService _connections;
        private readonly RemoteOcppCommandClient? _remote;

        public ChargePointStatusManagerService(WebSocketManagerService connections, RemoteOcppCommandClient? remote = null)
        {
            _connections = connections;
            _remote = remote;
        }

        /// <summary>True while the charge point has an open connection on any instance.</summary>
        public bool ChargePointExists(string chargePointId) =>
            _connections.GetConnection(chargePointId)?.IsOpen == true || _remote?.FindRemoteOwner(chargePointId) != null;

        /// <summary>
        /// The live status; for a charger on another instance only id and protocol are known
        /// (connector states are per connection and stay on the owning instance).
        /// </summary>
        public ChargePointStatus? GetChargePointStatus(string chargePointId)
        {
            var local = _connections.GetConnection(chargePointId)?.Status;
            if (local != null) return local;
            var owner = _remote?.FindRemoteOwner(chargePointId);
            return owner == null ? null : new ChargePointStatus { Id = chargePointId, Protocol = owner.ProtocolVersion };
        }
    }
}
