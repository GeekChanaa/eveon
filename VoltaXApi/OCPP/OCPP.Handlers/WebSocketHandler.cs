using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.ScaleOut;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>
    /// Owns a charger WebSocket from the accepted handshake until it closes: assembles frames, hands them to
    /// <see cref="OCPPMessageProcessor"/> and processes the charger's CALLs one after the other on a separate
    /// worker, so the receive loop keeps reading replies to CSMS commands while a handler runs. Singleton:
    /// database work at connect/disconnect runs in its own DI scope.
    /// </summary>
    public class WebSocketHandler
    {
        private const int ReceiveBufferSize = 4 * 1024;
        // A well-behaved charger has at most one CALL in flight; the rest is a safety margin.
        private const int MaxQueuedCalls = 16;

        private readonly WebSocketManagerService _connections;
        private readonly OcppPendingRequestRegistry _pending;
        private readonly OCPPMessageProcessor _msgProcessor;
        private readonly IHubContext<ChargerHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ChargePointConnectivityNotifier _connectivityNotifier;
        private readonly IConfiguration _config;
        private readonly ILogger<WebSocketHandler> _logger;
        private readonly ChargerOwnershipTracker? _ownership;
        private readonly FileWriter _fileWriter = new();
        private readonly int _maxMessageSize;

        public WebSocketHandler(
          WebSocketManagerService connections,
          OcppPendingRequestRegistry pending,
          OCPPMessageProcessor msgProcessor,
          IHubContext<ChargerHub> hubContext,
          IServiceScopeFactory scopeFactory,
          ChargePointConnectivityNotifier connectivityNotifier,
          IConfiguration config,
          ILogger<WebSocketHandler> logger,
          ChargerOwnershipTracker? ownership = null)
        {
            _ownership = ownership;
            _connections = connections;
            _pending = pending;
            _msgProcessor = msgProcessor;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _connectivityNotifier = connectivityNotifier;
            _config = config;
            _logger = logger;
            _maxMessageSize = Math.Max(ReceiveBufferSize, config.GetValue("Ocpp:MaxMessageSizeBytes", 1024 * 1024));
        }

        public async Task AcceptWebSocketAsync(HttpContext context, string subProtocol, ChargePointStatus chargePointStatus)
        {
            using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
            var stopping = context.RequestServices.GetService<IHostApplicationLifetime>()?.ApplicationStopping ?? CancellationToken.None;
            using var aborted = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, stopping);

            chargePointStatus.WebSocket = webSocket;
            chargePointStatus.Protocol = subProtocol;
            var connection = new OcppConnection(chargePointStatus.Id, subProtocol, webSocket, chargePointStatus, aborted.Token);
            await RunConnectionAsync(connection);
        }

        /// <summary>Registers the connection, runs it until it closes and always cleans up afterwards.</summary>
        public async Task RunConnectionAsync(OcppConnection connection)
        {
            var chargePointId = connection.ChargePointId;
            using var logScope = _logger.BeginScope(new Dictionary<string, object?> { ["ChargePointId"] = chargePointId });
            _logger.LogInformation("Charge point {ChargePointId} connected ({Protocol})", chargePointId, connection.ProtocolVersion);

            var previous = _connections.AddConnection(connection);
            if (previous != null && previous != connection)
            {
                _logger.LogInformation("Charge point {ChargePointId} reconnected: closing its previous connection", chargePointId);
                previous.Abort();
            }
            if (_ownership != null)
                await _ownership.OnConnectedAsync(connection);

            _connectivityNotifier.Connected(chargePointId);
            await RunInScope(chargePointId, "record the uptime start", sp => sp.GetRequiredService<IChargePointUptimeRepository>().StartOnline(chargePointId));

            var calls = Channel.CreateBounded<(OcppFrame Frame, string Raw)>(new BoundedChannelOptions(MaxQueuedCalls) { SingleReader = true, SingleWriter = true });
            var worker = ProcessCallsAsync(connection, calls.Reader);
            try
            {
                await ReceiveLoopAsync(connection, calls.Writer);
            }
            catch (Exception ex) when (ex is OperationCanceledException or WebSocketException)
            {
                _logger.LogInformation("Connection of {ChargePointId} ended: {Reason}", chargePointId, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Receive loop of {ChargePointId} failed", chargePointId);
            }
            finally
            {
                calls.Writer.TryComplete();
                connection.Abort();
                var stillCurrent = _connections.RemoveConnection(connection);
                var failed = _pending.FailAll(connection, new WebSocketNotFoundException($"Charge point {chargePointId} disconnected."));
                if (failed > 0) _logger.LogInformation("{Count} pending command(s) to {ChargePointId} failed: connection closed", failed, chargePointId);

                try { await worker; }
                catch (Exception ex) { _logger.LogError(ex, "Message worker of {ChargePointId} failed", chargePointId); }

                // A newer connection of the same charger is live, here or on another instance: it is not offline.
                var ownedHere = stillCurrent && (_ownership == null || await _ownership.OnDisconnectedAsync(connection));
                if (ownedHere)
                    await RunInScope(chargePointId, "mark the charge point offline", sp => sp.GetRequiredService<IChargePointService>().HandleChargePointDisconnected(chargePointId));
                if (ownedHere || !stillCurrent)
                    _connectivityNotifier.Disconnected(chargePointId);
                else
                    _logger.LogInformation("Charge point {ChargePointId} is now connected to another instance", chargePointId);
                _logger.LogInformation("Charge point {ChargePointId} disconnected (close status {CloseStatus})", chargePointId, connection.WebSocket.CloseStatus);
            }
        }

        private async Task ReceiveLoopAsync(OcppConnection connection, ChannelWriter<(OcppFrame Frame, string Raw)> calls)
        {
            var webSocket = connection.WebSocket;
            var token = connection.Closed;
            var buffer = new byte[ReceiveBufferSize];
            using var message = new MemoryStream();
            var skipping = false;

            while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("{ChargePointId} closed the connection: {CloseStatus} {CloseDescription}",
                        connection.ChargePointId, result.CloseStatus, result.CloseStatusDescription);
                    await connection.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // OCPP-J only uses text frames.
                    if (!skipping) _logger.LogWarning("Ignoring binary frame from {ChargePointId}", connection.ChargePointId);
                    skipping = !result.EndOfMessage;
                    continue;
                }

                if (skipping)
                {
                    skipping = !result.EndOfMessage;
                    continue;
                }

                if (message.Length + result.Count > _maxMessageSize)
                {
                    _logger.LogWarning("{ChargePointId} sent a message larger than {MaxBytes} bytes: closing", connection.ChargePointId, _maxMessageSize);
                    await connection.CloseOutputAsync(WebSocketCloseStatus.MessageTooBig, $"Message exceeds {_maxMessageSize} bytes", CancellationToken.None);
                    return;
                }

                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage) continue;

                var bytes = message.ToArray();
                message.SetLength(0);
                await OnMessageAsync(connection, bytes, calls);
            }
        }

        private async Task OnMessageAsync(OcppConnection connection, byte[] bytes, ChannelWriter<(OcppFrame Frame, string Raw)> calls)
        {
            DumpMessage(bytes, "incoming");
            string text;
            try
            {
                text = new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                _logger.LogWarning("Ignoring a frame from {ChargePointId} that is not valid UTF-8", connection.ChargePointId);
                return;
            }

            _logger.LogDebug("Received from {ChargePointId}: {Frame}", connection.ChargePointId, text);
            try
            {
                await _hubContext.Clients.Group(connection.ChargePointId).SendAsync("SentMessage", JsonConvert.SerializeObject(text), connection.Closed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not mirror an incoming frame of {ChargePointId} to the dashboard", connection.ChargePointId);
            }

            var call = await _msgProcessor.ProcessIncomingAsync(connection, text, connection.Closed);
            if (call == null) return;

            if (!calls.TryWrite((call, text)))
            {
                _logger.LogWarning("{ChargePointId} sent too many CALLs at once: rejecting {Action} (message {MessageId})", connection.ChargePointId, call.Action, call.UniqueId);
                await _msgProcessor.SendFrameAsync(connection,
                    OCPPRequestHandler.CallError(connection, call.UniqueId, OcppError.GenericError, "Too many concurrent requests."), connection.Closed);
            }
        }

        private async Task ProcessCallsAsync(OcppConnection connection, ChannelReader<(OcppFrame Frame, string Raw)> calls)
        {
            // Without a token: CALLs already received are still stored after a disconnect; only their replies are lost.
            await foreach (var (frame, raw) in calls.ReadAllAsync())
            {
                try
                {
                    await _msgProcessor.HandleCallAsync(connection, frame, raw, connection.Closed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Processing {Action} (message {MessageId}) from {ChargePointId} failed", frame.Action, frame.UniqueId, connection.ChargePointId);
                }
            }
        }

        private async Task RunInScope(string chargePointId, string what, Func<IServiceProvider, Task> work)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await work(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not {What} for {ChargePointId}", what, chargePointId);
            }
        }

        private void DumpMessage(byte[] message, string direction)
        {
            string? dumpDir = _config.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-ffff}_{direction}.txt";
                _fileWriter.WriteMessageToFile(dumpDir, fileName, message);
            }
        }
    }
}
