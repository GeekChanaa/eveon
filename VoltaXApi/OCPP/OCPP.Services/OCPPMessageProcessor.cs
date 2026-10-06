using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.ScaleOut;

namespace VoltaXApi.OCPP.Core
{
    /// <summary>Sends CSMS-initiated CALLs and waits for the charger's answer. Version-agnostic: works with any message types.</summary>
    public interface IOcppCommandSender
    {
        /// <summary>
        /// Sends <paramref name="request"/> as a CALL and returns the charger's CALLRESULT deserialized to <typeparamref name="TResponse"/>.
        /// Throws <see cref="WebSocketNotFoundException"/> when the charger is not connected (or disconnects before answering),
        /// <see cref="OcppCallErrorException"/> on a CALLERROR and <see cref="TimeoutException"/> when no answer arrives within
        /// <paramref name="timeout"/> (default: Ocpp:CommandTimeoutSeconds, 30 s).
        /// </summary>
        Task<TResponse> SendRequestAsync<TRequest, TResponse>(string chargePointId, string action, TRequest request,
            TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>The sub-protocol of the charger's live connection ("ocpp2.0.1", "ocpp1.6"), or null when it is offline.</summary>
        string? GetProtocolVersion(string chargePointId);
    }

    /// <summary>
    /// OCPP-J message layer of every charger connection: parses incoming frames, hands CALLRESULT/CALLERROR to the
    /// waiting request and writes every outgoing frame (through the connection's send lock). Singleton.
    /// </summary>
    public class OCPPMessageProcessor : IOcppCommandSender
    {
        private readonly WebSocketManagerService _connections;
        private readonly OcppPendingRequestRegistry _pending;
        private readonly OCPPRequestHandler _requestHandler;
        private readonly IHubContext<ChargerHub> _hubContext;
        private readonly ILogger<OCPPMessageProcessor> _logger;
        private readonly RemoteOcppCommandClient? _remote;
        private readonly TimeSpan _defaultTimeout;

        public OCPPMessageProcessor(
            IConfiguration config,
            WebSocketManagerService connections,
            OcppPendingRequestRegistry pending,
            OCPPRequestHandler requestHandler,
            IHubContext<ChargerHub> hubContext,
            ILogger<OCPPMessageProcessor> logger,
            RemoteOcppCommandClient? remote = null)
        {
            _remote = remote;
            _connections = connections;
            _pending = pending;
            _requestHandler = requestHandler;
            _hubContext = hubContext;
            _logger = logger;
            var seconds = config.GetValue("Ocpp:CommandTimeoutSeconds", 30);
            _defaultTimeout = TimeSpan.FromSeconds(seconds > 0 ? seconds : 30);
        }

        public TimeSpan DefaultTimeout => _defaultTimeout;

        public string? GetProtocolVersion(string chargePointId) =>
            _connections.GetConnection(chargePointId)?.ProtocolVersion ?? _remote?.FindRemoteOwner(chargePointId)?.ProtocolVersion;

        /// <summary>
        /// Handles one complete text frame from the charger. Replies are matched to their pending request right away;
        /// a valid CALL is returned so the caller can queue it (CALLs are processed one after the other, off the receive loop).
        /// Malformed frames are answered with a CALLERROR when their message id can be read, otherwise ignored.
        /// </summary>
        public async Task<OcppFrame?> ProcessIncomingAsync(OcppConnection connection, string text, CancellationToken cancellationToken)
        {
            var parsed = OcppJson.Parse(text);
            if (!parsed.Success)
            {
                var isReply = parsed.MessageType is (int)OcppMessageType.CallResult or (int)OcppMessageType.CallError;
                if (parsed.UniqueId == null || isReply)
                {
                    _logger.LogWarning("Ignoring malformed frame from {ChargePointId} (message {MessageId}): {Reason}",
                        connection.ChargePointId, parsed.UniqueId, parsed.ErrorDescription);
                    return null;
                }

                _logger.LogWarning("Malformed frame {MessageId} from {ChargePointId}: {Reason}", parsed.UniqueId, connection.ChargePointId, parsed.ErrorDescription);
                await TrySendAsync(connection, OCPPRequestHandler.CallError(connection, parsed.UniqueId, parsed.Error, parsed.ErrorDescription!), cancellationToken);
                return null;
            }

            var frame = parsed.Frame!;
            switch (frame.MessageType)
            {
                case OcppMessageType.Call:
                    return frame;

                case OcppMessageType.CallResult:
                    if (_pending.TryComplete(connection, frame.UniqueId, frame.Payload, out var action))
                        _logger.LogDebug("{ChargePointId} answered {Action} (message {MessageId})", connection.ChargePointId, action, frame.UniqueId);
                    else
                        _logger.LogWarning("CALLRESULT {MessageId} from {ChargePointId} matches no pending request (late or unknown)", frame.UniqueId, connection.ChargePointId);
                    return null;

                default:
                    var error = new OcppCallErrorException(frame.ErrorCode, frame.ErrorDescription, frame.Payload);
                    if (_pending.TryFail(connection, frame.UniqueId, error, out var failedAction))
                        _logger.LogWarning("{ChargePointId} answered {Action} (message {MessageId}) with CALLERROR {ErrorCode}: {ErrorDescription}",
                            connection.ChargePointId, failedAction, frame.UniqueId, frame.ErrorCode, frame.ErrorDescription);
                    else
                        _logger.LogWarning("CALLERROR {MessageId} ({ErrorCode}) from {ChargePointId} matches no pending request", frame.UniqueId, frame.ErrorCode, connection.ChargePointId);
                    return null;
            }
        }

        /// <summary>Runs the handler of a queued CALL and sends its CALLRESULT/CALLERROR.</summary>
        public async Task HandleCallAsync(OcppConnection connection, OcppFrame call, string rawMessage, CancellationToken cancellationToken)
        {
            var reply = await _requestHandler.ProcessRequest(connection, call, rawMessage);
            await TrySendAsync(connection, reply, cancellationToken);
        }

        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(string chargePointId, string action, TRequest request,
            TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var payload = JsonConvert.SerializeObject(request, OCPPMessageFactory.DefaultSettings);
            var answer = await SendCallAsync(chargePointId, action, payload, timeout, cancellationToken);
            try
            {
                return JsonConvert.DeserializeObject<TResponse>(answer, OCPPMessageFactory.DefaultSettings)
                    ?? throw new JsonSerializationException("Empty response payload.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "{ChargePointId} sent an unreadable {Action} response", chargePointId, action);
                throw new OcppCallErrorException(OcppErrors.ToWireName(OcppError.FormationViolation, GetProtocolVersion(chargePointId)),
                    $"The charger's {action} response could not be read.");
            }
        }

        /// <summary>Legacy overload taking a prepared message; returns the raw CALLRESULT payload.</summary>
        public Task<string> SendRequestAsync(OCPPMessage message, string chargePointID, TimeSpan timeout, CancellationToken cancellationToken = default) =>
            SendCallAsync(chargePointID, message.Action, message.JsonPayload, timeout, cancellationToken);

        /// <summary>
        /// Sends a CALL with an already serialized payload and returns the raw CALLRESULT payload. A charger connected
        /// to another instance is reached through that instance (scale-out), with the same exceptions.
        /// </summary>
        public Task<string> SendCallAsync(string chargePointId, string action, string? payloadJson, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            // A 2.0.1-only command to a 1.6 charger (or the reverse) would only come back as a CALLERROR.
            var version = GetProtocolVersion(chargePointId);
            if (version != null && !OcppProtocols.SupportsOutbound(version, action))
                throw OcppProtocolNotSupportedException.ForCommand(action, version);

            var connection = _connections.GetConnection(chargePointId);
            if ((connection == null || !connection.IsOpen) && _remote != null)
                return _remote.SendAsync(chargePointId, action, payloadJson, timeout ?? _defaultTimeout, cancellationToken);
            return SendLocalCallAsync(chargePointId, action, payloadJson, timeout, cancellationToken);
        }

        /// <summary>Sends a CALL on this instance's own connection of the charger.</summary>
        public async Task<string> SendLocalCallAsync(string chargePointId, string action, string? payloadJson, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var connection = _connections.GetConnection(chargePointId);
            if (connection == null || !connection.IsOpen)
                throw new WebSocketNotFoundException($"Charge point {chargePointId} is not connected.");

            var effectiveTimeout = timeout ?? _defaultTimeout;
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connection.Closed);
            waitCts.CancelAfter(effectiveTimeout);

            try
            {
                // One outstanding CALL per charger; the wait counts towards the timeout.
                await connection.CallLock.WaitAsync(waitCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw Unanswered(connection, action, effectiveTimeout);
            }

            var uniqueId = Guid.NewGuid().ToString("N");
            try
            {
                var reply = _pending.Register(connection, uniqueId, action);
                try
                {
                    _logger.LogInformation("Sending {Action} to {ChargePointId} (message {MessageId})", action, chargePointId, uniqueId);
                    await SendFrameAsync(connection, OcppJson.SerializeCall(uniqueId, action, payloadJson), waitCts.Token);
                    return await reply.WaitAsync(waitCts.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw Unanswered(connection, action, effectiveTimeout);
                }
                finally
                {
                    _pending.Remove(uniqueId);
                }
            }
            finally
            {
                connection.CallLock.Release();
            }
        }

        private Exception Unanswered(OcppConnection connection, string action, TimeSpan timeout)
        {
            if (connection.Closed.IsCancellationRequested)
                return new WebSocketNotFoundException($"Charge point {connection.ChargePointId} disconnected before answering {action}.");
            _logger.LogWarning("{ChargePointId} did not answer {Action} within {Timeout} s", connection.ChargePointId, action, timeout.TotalSeconds);
            return new TimeoutException($"Charge point {connection.ChargePointId} did not answer {action} within {timeout.TotalSeconds:0} s.");
        }

        /// <summary>Writes one frame through the connection's send lock and mirrors it to the dashboard hub.</summary>
        public async Task SendFrameAsync(OcppConnection connection, string frame, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Sending to {ChargePointId}: {Frame}", connection.ChargePointId, frame);
            await connection.SendTextAsync(frame, cancellationToken);
            try
            {
                await _hubContext.Clients.Group(connection.ChargePointId).SendAsync("ReceiveMessage", frame, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not mirror an outgoing frame of {ChargePointId} to the dashboard", connection.ChargePointId);
            }
        }

        // Replies to the charger: a closed socket only means the reply is lost, which the charger handles by retrying.
        private async Task TrySendAsync(OcppConnection connection, string frame, CancellationToken cancellationToken)
        {
            try
            {
                await SendFrameAsync(connection, frame, cancellationToken);
            }
            catch (Exception ex) when (ex is WebSocketNotFoundException or System.Net.WebSockets.WebSocketException or OperationCanceledException)
            {
                _logger.LogInformation("Reply to {ChargePointId} not sent, connection closed: {Reason}", connection.ChargePointId, ex.Message);
            }
        }
    }
}
