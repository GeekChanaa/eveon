using System.Net.WebSockets;
using System.Text;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Exceptions;

namespace VoltaXApi.OCPP.Models
{
    /// <summary>
    /// One live charger WebSocket. Lives as long as the socket; every send goes through it so that
    /// handler replies and API-initiated commands never call <see cref="WebSocket.SendAsync"/> concurrently.
    /// </summary>
    public sealed class OcppConnection
    {
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly CancellationTokenSource _lifetime;

        public OcppConnection(string chargePointId, string protocolVersion, WebSocket webSocket, ChargePointStatus status, CancellationToken connectionAborted = default)
        {
            ChargePointId = chargePointId;
            ProtocolVersion = protocolVersion;
            WebSocket = webSocket;
            Status = status;
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(connectionAborted);
        }

        public string ChargePointId { get; }

        /// <summary>The sub-protocol negotiated at the handshake, e.g. "ocpp2.0.1" (see <see cref="OcppProtocols"/>).</summary>
        public string ProtocolVersion { get; }

        public WebSocket WebSocket { get; }

        /// <summary>Per-connection state handed to the inbound handlers.</summary>
        public ChargePointStatus Status { get; }

        public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;

        /// <summary>Cancelled when the connection ends (socket closed, request aborted, superseded or server stopping).</summary>
        public CancellationToken Closed => _lifetime.Token;

        public bool IsOpen => WebSocket.State == WebSocketState.Open && !_lifetime.IsCancellationRequested;

        /// <summary>Held by the CSMS while one of its CALLs is outstanding: OCPP-J allows a single pending CALL per direction.</summary>
        internal SemaphoreSlim CallLock { get; } = new(1, 1);

        public async Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (WebSocket.State != WebSocketState.Open)
                    throw new WebSocketNotFoundException($"The connection of charge point {ChargePointId} is not open.");
                await WebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>Sends a close frame (serialized with the other sends). Does nothing when the socket is already closing.</summary>
        public async Task CloseOutputAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken)
        {
            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (WebSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await WebSocket.CloseOutputAsync(status, description, cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>Ends the connection: pending waits and the receive loop observe <see cref="Closed"/>.</summary>
        public void Abort()
        {
            try { _lifetime.Cancel(); }
            catch (ObjectDisposedException) { }
        }
    }
}
