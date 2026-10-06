using System.Globalization;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Ocpp16
{
    /// <summary>
    /// CSMS -> charger commands for OCPP 1.6 chargers. The existing (2.0.1-shaped) command services route here when
    /// <see cref="IsOcpp16"/> is true; the 2.0.1 request is translated and the 1.6 answer mapped back to the 2.0.1
    /// response type so controllers and the dashboard work unchanged. Options 1.6 cannot express throw
    /// <see cref="OcppProtocolNotSupportedException"/> (HTTP 400).
    /// </summary>
    public class Ocpp16CommandService
    {
        private readonly IOcppCommandSender _sender;
        private readonly ILogger<Ocpp16CommandService> _logger;
        private readonly IServiceProvider _services;

        public Ocpp16CommandService(IOcppCommandSender sender, ILogger<Ocpp16CommandService> logger, IServiceProvider services)
        {
            _sender = sender;
            _services = services;
            _logger = logger;
        }

        public bool IsOcpp16(string chargePointId) => _sender.GetProtocolVersion(chargePointId) == OcppProtocols.Ocpp16;

        /// <summary>Throws when the charger is connected with OCPP 1.6 (for 2.0.1-only commands).</summary>
        public void Require201(string chargePointId, string action)
        {
            if (IsOcpp16(chargePointId))
                throw OcppProtocolNotSupportedException.ForCommand(action, OcppProtocols.Ocpp16);
        }

        /// <summary>Throws when the charger is connected with another version than 1.6 (for 1.6-only commands).</summary>
        public void Require16(string chargePointId, string action)
        {
            var version = _sender.GetProtocolVersion(chargePointId);
            if (version != null && version != OcppProtocols.Ocpp16)
                throw OcppProtocolNotSupportedException.ForCommand(action, version);
        }

        private Task<TResponse> Send<TRequest, TResponse>(string chargePointId, string action, TRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending {Action} (1.6) to {ChargePointId}", action, chargePointId);
            return _sender.SendRequestAsync<TRequest, TResponse>(chargePointId, action, request, cancellationToken: cancellationToken);
        }

        private static TEnum ParseStatus<TEnum>(string? status, TEnum fallback) where TEnum : struct, Enum =>
            Enum.TryParse<TEnum>(status, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed) ? parsed : fallback;

        private static StatusInfoType? Reason(string? status) => status == null ? null : new StatusInfoType { ReasonCode = status };

        // ---------------------------------------------------------------- EV driver
        public async Task<RequestStartTransactionResponse> RemoteStartTransaction(string chargePointId, RequestStartTransactionRequest request, CancellationToken cancellationToken)
        {
            var idTag = request.IdToken?.IdToken;
            if (string.IsNullOrWhiteSpace(idTag))
                throw new OcppProtocolNotSupportedException("OCPP 1.6 RemoteStartTransaction requires an idTag.");
            if (request.ChargingProfile != null)
                throw new OcppProtocolNotSupportedException("A charging profile on remote start is not supported for OCPP 1.6 chargers; use SetChargingProfile.");

            var answer = await Send<RemoteStartTransaction16Request, StatusResponse16>(chargePointId, "RemoteStartTransaction", new RemoteStartTransaction16Request
            {
                IdTag = idTag,
                ConnectorId = request.EvseId ?? request.Evse?.Id ?? request.ConnectorId
            }, cancellationToken);
            return new RequestStartTransactionResponse { Status = ParseStatus(answer.Status, RequestStartStopStatusEnum.Rejected) };
        }

        /// <summary>The transaction id is the "ocpp16-N" uid of our transaction or the charger's integer id.</summary>
        public async Task<RequestStopTransactionResponse> RemoteStopTransaction(string chargePointId, RequestStopTransactionRequest request, CancellationToken cancellationToken)
        {
            if (!Ocpp16Transaction.TryParseUid(request.TransactionId, out var transactionId))
                return new RequestStopTransactionResponse
                {
                    Status = RequestStartStopStatusEnum.Rejected,
                    StatusInfo = new StatusInfo { ReasonCode = "UnknownTransaction", AdditionalInfo = "Not an OCPP 1.6 transaction id." }
                };

            var answer = await Send<RemoteStopTransaction16Request, StatusResponse16>(chargePointId, "RemoteStopTransaction",
                new RemoteStopTransaction16Request { TransactionId = transactionId }, cancellationToken);
            return new RequestStopTransactionResponse { Status = ParseStatus(answer.Status, RequestStartStopStatusEnum.Rejected) };
        }

        public async Task<UnlockConnectorResponse> UnlockConnector(string chargePointId, UnlockConnectorRequest request, CancellationToken cancellationToken)
        {
            var answer = await Send<UnlockConnector16Request, StatusResponse16>(chargePointId, "UnlockConnector",
                new UnlockConnector16Request { ConnectorId = request.EvseId }, cancellationToken);
            // 1.6 NotSupported has no 2.0.1 counterpart: UnlockFailed with the original status as reason.
            return answer.Status == "Unlocked"
                ? new UnlockConnectorResponse { Status = UnlockStatusEnumType.Unlocked }
                : new UnlockConnectorResponse { Status = UnlockStatusEnumType.UnlockFailed, StatusInfo = Reason(answer.Status) };
        }

        public async Task<ReserveNowResponse> ReserveNow(string chargePointId, ReserveNowRequest request, CancellationToken cancellationToken)
        {
            var answer = await Send<ReserveNow16Request, StatusResponse16>(chargePointId, "ReserveNow", new ReserveNow16Request
            {
                ReservationId = request.Id,
                ConnectorId = request.EvseId ?? 0,
                ExpiryDate = Ocpp16Mapping.ToUtc(request.ExpiryDateTime),
                IdTag = request.IdToken.IdToken,
                ParentIdTag = request.GroupIdToken?.IdToken
            }, cancellationToken);
            return new ReserveNowResponse { Status = ParseStatus(answer.Status, ReserveNowStatusEnumType.Rejected) };
        }

        public async Task<CancelReservationResponse> CancelReservation(string chargePointId, CancelReservationRequest request, CancellationToken cancellationToken)
        {
            var answer = await Send<CancelReservation16Request, StatusResponse16>(chargePointId, "CancelReservation",
                new CancelReservation16Request { ReservationId = request.ReservationId }, cancellationToken);
            return new CancelReservationResponse { Status = ParseStatus(answer.Status, CancelReservationStatusEnumType.Rejected) };
        }

        public async Task<ClearCacheResponse> ClearCache(string chargePointId, CancellationToken cancellationToken)
        {
            var answer = await Send<ClearCache16Request, StatusResponse16>(chargePointId, "ClearCache", new ClearCache16Request(), cancellationToken);
            return new ClearCacheResponse { Status = ParseStatus(answer.Status, ClearCacheStatusEnum.Rejected) };
        }

        public async Task<SendLocalListResponse> SendLocalList(string chargePointId, SendLocalListRequest request, CancellationToken cancellationToken)
        {
            var answer = await Send<SendLocalList16Request, StatusResponse16>(chargePointId, "SendLocalList", new SendLocalList16Request
            {
                ListVersion = request.VersionNumber,
                UpdateType = request.UpdateType.ToString(),
                LocalAuthorizationList = request.LocalAuthorizationList?.Select(entry => new AuthorizationData16
                {
                    IdTag = entry.IdToken.IdToken,
                    // No idTagInfo in a Differential update removes the entry.
                    IdTagInfo = entry.IdTokenInfo == null ? null : new IdTagInfo { Status = Ocpp16Mapping.ToIdTagStatus(entry.IdTokenInfo.Status) }
                }).ToList()
            }, cancellationToken);
            return answer.Status == "NotSupported"
                ? new SendLocalListResponse { Status = SendLocalListStatusEnumType.Failed, StatusInfo = Reason(answer.Status)! }
                : new SendLocalListResponse { Status = ParseStatus(answer.Status, SendLocalListStatusEnumType.Failed) };
        }

        public async Task<GetLocalListVersionResponse> GetLocalListVersion(string chargePointId, CancellationToken cancellationToken)
        {
            var answer = await Send<GetLocalListVersion16Request, GetLocalListVersion16Response>(chargePointId, "GetLocalListVersion",
                new GetLocalListVersion16Request(), cancellationToken);
            return new GetLocalListVersionResponse { VersionNumber = answer.ListVersion };
        }

        // ---------------------------------------------------------------- configuration / firmware
        public async Task<ResetResponse> Reset(string chargePointId, ResetRequest request, CancellationToken cancellationToken)
        {
            if (request.EvseId.HasValue)
                throw new OcppProtocolNotSupportedException("Resetting a single EVSE is not supported by OCPP 1.6 chargers.");
            var answer = await Send<Reset16Request, StatusResponse16>(chargePointId, "Reset",
                new Reset16Request { Type = request.Type == ResetEnumType.Immediate ? "Hard" : "Soft" }, cancellationToken);
            return new ResetResponse { Status = ParseStatus(answer.Status, ResetStatusEnumType.Rejected) };
        }

        public async Task<ChangeAvailabilityResponse> ChangeAvailability(string chargePointId, ChangeAvailabilityRequest request, CancellationToken cancellationToken)
        {
            var answer = await Send<ChangeAvailability16Request, StatusResponse16>(chargePointId, "ChangeAvailability", new ChangeAvailability16Request
            {
                ConnectorId = request.Evse?.Id ?? 0,
                Type = request.OperationalStatus.ToString()
            }, cancellationToken);
            return new ChangeAvailabilityResponse { Status = ParseStatus(answer.Status, ChangeAvailabilityStatusEnumType.Rejected) };
        }

        public async Task<TriggerMessageResponse> TriggerMessage(string chargePointId, TriggerMessageRequest request, CancellationToken cancellationToken)
        {
            var message = request.RequestedMessage switch
            {
                MessageTriggerEnumType.BootNotification => "BootNotification",
                MessageTriggerEnumType.Heartbeat => "Heartbeat",
                MessageTriggerEnumType.MeterValues => "MeterValues",
                MessageTriggerEnumType.StatusNotification => "StatusNotification",
                MessageTriggerEnumType.FirmwareStatusNotification => "FirmwareStatusNotification",
                MessageTriggerEnumType.LogStatusNotification => "DiagnosticsStatusNotification",
                _ => throw new OcppProtocolNotSupportedException($"TriggerMessage {request.RequestedMessage} is not supported by OCPP 1.6 chargers.")
            };
            var answer = await Send<TriggerMessage16Request, StatusResponse16>(chargePointId, "TriggerMessage", new TriggerMessage16Request
            {
                RequestedMessage = message,
                ConnectorId = request.Evse?.Id is > 0 ? request.Evse.Id : null
            }, cancellationToken);
            return new TriggerMessageResponse { Status = ParseStatus(answer.Status, TriggerMessageStatusEnumType.Rejected) };
        }

        /// <summary>1.6 has no inventory report: the connectors are (re)announced by their StatusNotifications.</summary>
        public async Task<GetBaseReportResponse> RefreshConnectors(string chargePointId, CancellationToken cancellationToken)
        {
            var answer = await TriggerMessage(chargePointId, new TriggerMessageRequest { RequestedMessage = MessageTriggerEnumType.StatusNotification }, cancellationToken);
            return new GetBaseReportResponse
            {
                Status = answer.Status switch
                {
                    TriggerMessageStatusEnumType.Accepted => GenericDeviceModelStatusEnumType.Accepted,
                    TriggerMessageStatusEnumType.NotImplemented => GenericDeviceModelStatusEnumType.NotSupported,
                    _ => GenericDeviceModelStatusEnumType.Rejected
                }
            };
        }

        public async Task<UpdateFirmwareResponse> UpdateFirmware(string chargePointId, UpdateFirmwareRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.Firmware?.Signature) || !string.IsNullOrEmpty(request.Firmware?.SigningCertificate))
                throw new OcppProtocolNotSupportedException("Signed firmware updates are not supported for OCPP 1.6 chargers.");
            if (string.IsNullOrWhiteSpace(request.Firmware?.Location))
                throw new OcppProtocolNotSupportedException("OCPP 1.6 UpdateFirmware requires a firmware location.");
            var retrieveDate = DateTime.TryParse(request.Firmware.RetrieveDateTime, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : DateTime.UtcNow;

            // UpdateFirmware.conf has no fields in 1.6: an answer means the charger accepted the request.
            await Send<UpdateFirmware16Request, Empty16Response>(chargePointId, "UpdateFirmware", new UpdateFirmware16Request
            {
                Location = request.Firmware.Location,
                RetrieveDate = retrieveDate,
                Retries = request.Retries,
                RetryInterval = request.RetryInterval
            }, cancellationToken);
            return new UpdateFirmwareResponse { Status = UpdateFirmwareStatusEnumType.Accepted };
        }

        // ---------------------------------------------------------------- 1.6 only
        /// <summary>Without a location the charger uploads to a one-time URL from <see cref="ILogUploadUrlFactory"/>.</summary>
        public async Task<GetDiagnostics16Response> GetDiagnostics(string chargePointId, GetDiagnostics16Request request, CancellationToken cancellationToken)
        {
            Require16(chargePointId, "GetDiagnostics");
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                // 1.6 has no requestId: the ticket gets a random one, statuses go to the charger's latest ticket.
                var upload = await _services.GetRequiredService<ILogUploadUrlFactory>()
                    .CreateAsync(chargePointId, Random.Shared.Next(1, int.MaxValue), LogUploadPurposes.Diagnostics, cancellationToken);
                request.Location = upload.Url;
            }
            request.StartTime = request.StartTime.HasValue ? Ocpp16Mapping.ToUtc(request.StartTime.Value) : null;
            request.StopTime = request.StopTime.HasValue ? Ocpp16Mapping.ToUtc(request.StopTime.Value) : null;
            return await Send<GetDiagnostics16Request, GetDiagnostics16Response>(chargePointId, "GetDiagnostics", request, cancellationToken);
        }

        public Task<StatusResponse16> ChangeConfiguration(string chargePointId, ChangeConfigurationDto request, CancellationToken cancellationToken)
        {
            Require16(chargePointId, "ChangeConfiguration");
            if (string.IsNullOrWhiteSpace(request.Key) || request.Key.Length > 50 || (request.Value?.Length ?? 0) > 500)
                throw new OcppProtocolNotSupportedException("ChangeConfiguration requires a key (max 50 characters) and a value (max 500 characters).");
            return Send<ChangeConfiguration16Request, StatusResponse16>(chargePointId, "ChangeConfiguration",
                new ChangeConfiguration16Request { Key = request.Key, Value = request.Value ?? "" }, cancellationToken);
        }

        public Task<GetConfiguration16Response> GetConfiguration(string chargePointId, GetConfigurationDto request, CancellationToken cancellationToken)
        {
            Require16(chargePointId, "GetConfiguration");
            var keys = request.Keys?.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).Distinct().ToList();
            return Send<GetConfiguration16Request, GetConfiguration16Response>(chargePointId, "GetConfiguration",
                new GetConfiguration16Request { Key = keys is { Count: > 0 } ? keys : null }, cancellationToken);
        }
    }
}
