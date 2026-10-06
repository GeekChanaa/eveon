using VoltaXApi.Data;
using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>
    /// Runs one charger-initiated CALL: resolves its handler by (protocol version, action) from a fresh DI scope
    /// (so every message gets its own DbContext) and turns the outcome into the CALLRESULT or CALLERROR frame.
    /// </summary>
    public class OCPPRequestHandler
    {
        public const string VENDOR_ID = "VoltaX Charging";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OcppInboundHandlerRegistry _registry;
        private readonly ILogger<OCPPRequestHandler> _logger;

        public OCPPRequestHandler(IServiceScopeFactory scopeFactory, OcppInboundHandlerRegistry registry, ILogger<OCPPRequestHandler> logger)
        {
            _scopeFactory = scopeFactory;
            _registry = registry;
            _logger = logger;
        }

        /// <summary>Returns the serialized reply frame. Never throws for handler failures.</summary>
        public async Task<string> ProcessRequest(OcppConnection connection, OcppFrame call, string rawMessage)
        {
            var msgIn = new OCPPMessage("2", call.UniqueId, call.Action!, call.Payload, rawMessage);
            var msgOut = new OCPPMessage { MessageType = "3", UniqueId = call.UniqueId, Action = call.Action! };

            using var logScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["ChargePointId"] = connection.ChargePointId,
                ["OcppAction"] = call.Action,
                ["OcppMessageId"] = call.UniqueId
            });

            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = _registry.Resolve(scope.ServiceProvider, connection.ProtocolVersion, call.Action!);
            if (handler == null)
            {
                _logger.LogWarning("No {Protocol} handler for action {Action} from {ChargePointId}", connection.ProtocolVersion, call.Action, connection.ChargePointId);
                msgOut.MessageType = "4";
                msgOut.ErrorCode = ErrorCodes.NotImplemented;
                await TryLog(scope.ServiceProvider, connection, msgIn, msgOut);
                return CallError(connection, call.UniqueId, OcppError.NotImplemented, $"Action {call.Action} is not implemented.");
            }

            string? errorCode;
            try
            {
                errorCode = await handler.Handle(msgIn, msgOut, connection.Status);
            }
            catch (Exception ex)
            {
                // The charger only gets a generic description; details stay in the server log.
                _logger.LogError(ex, "{Action} handler failed for {ChargePointId} (message {MessageId})", call.Action, connection.ChargePointId, call.UniqueId);
                msgOut.MessageType = "4";
                msgOut.ErrorCode = ErrorCodes.InternalError;
                await TryLogInNewScope(connection, msgIn, msgOut);
                return CallError(connection, call.UniqueId, OcppError.InternalError, "An internal error occurred while processing the request.");
            }

            if (!string.IsNullOrEmpty(errorCode))
            {
                var error = OcppErrors.Parse(errorCode);
                _logger.LogInformation("{Action} from {ChargePointId} answered with CALLERROR {ErrorCode}", call.Action, connection.ChargePointId, error);
                return CallError(connection, call.UniqueId, error, OcppErrors.DefaultDescription(error));
            }

            return OcppJson.SerializeCallResult(call.UniqueId, msgOut.JsonPayload);
        }

        public static string CallError(OcppConnection connection, string uniqueId, OcppError error, string description) =>
            OcppJson.SerializeCallError(uniqueId, OcppErrors.ToWireName(error, connection.ProtocolVersion), description);

        private async Task TryLog(IServiceProvider services, OcppConnection connection, OCPPMessage msgIn, OCPPMessage msgOut)
        {
            try
            {
                var repository = services.GetRequiredService<IMessageLogRepository>();
                await repository.SaveLogMessage(connection.ChargePointId, null, msgIn.Action, msgIn.JsonPayload, msgOut.ErrorCode, msgIn, msgOut);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not store the message log of {Action} from {ChargePointId}", msgIn.Action, connection.ChargePointId);
            }
        }

        // The handler's DbContext may be unusable after its exception.
        private async Task TryLogInNewScope(OcppConnection connection, OCPPMessage msgIn, OCPPMessage msgOut)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await TryLog(scope.ServiceProvider, connection, msgIn, msgOut);
        }
    }
}
