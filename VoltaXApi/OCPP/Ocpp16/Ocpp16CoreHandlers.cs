using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Ocpp16
{
    public class BootNotification16Handler : Ocpp16HandlerBase<BootNotification16Request, BootNotification16Response>
    {
        private readonly ChargePointBootService _bootService;

        public BootNotification16Handler(IMessageLogRepository messageLog, ILogger<BootNotification16Handler> logger, ChargePointBootService bootService)
            : base(messageLog, logger)
        {
            _bootService = bootService;
        }

        protected override async Task<Ocpp16Outcome<BootNotification16Response>> Process(BootNotification16Request request, ChargePointStatus chargePointStatus)
        {
            Logger.LogInformation("BootNotification (1.6) => {ChargePointId} {Vendor}/{Model} firmware {Firmware}",
                chargePointStatus.Id, request.ChargePointVendor, request.ChargePointModel, request.FirmwareVersion);

            var decision = await _bootService.RegisterBootAsync(chargePointStatus, new BootNotificationRequest
            {
                Reason = BootReasonEnumType.Unknown,
                ChargingStation = new ChargingStationType
                {
                    Model = request.ChargePointModel,
                    VendorName = request.ChargePointVendor,
                    SerialNumber = request.ChargePointSerialNumber ?? request.ChargeBoxSerialNumber!,
                    FirmwareVersion = request.FirmwareVersion!
                }
            });

            Logger.LogInformation("BootNotification (1.6) => {ChargePointId} answered {Status}", chargePointStatus.Id, decision.Status);
            return new(new BootNotification16Response
            {
                Status = decision.Status.ToString(),
                CurrentTime = DateTime.UtcNow,
                Interval = decision.Interval
            }, decision.Status.ToString());
        }
    }

    public class Heartbeat16Handler : Ocpp16HandlerBase<Empty16Response, Heartbeat16Response>
    {
        public Heartbeat16Handler(IMessageLogRepository messageLog, ILogger<Heartbeat16Handler> logger) : base(messageLog, logger)
        {
        }

        protected override Task<Ocpp16Outcome<Heartbeat16Response>> Process(Empty16Response request, ChargePointStatus chargePointStatus) =>
            Task.FromResult(new Ocpp16Outcome<Heartbeat16Response>(new Heartbeat16Response { CurrentTime = DateTime.UtcNow }));
    }

    /// <summary>
    /// 1.6 StatusNotification: connector 0 is the charger itself (logged only); other connectors update our connector
    /// status model (Preparing/Charging/Suspended*/Finishing = Occupied) like the 2.0.1 handler does.
    /// </summary>
    public class StatusNotification16Handler : Ocpp16HandlerBase<StatusNotification16Request, Empty16Response>
    {
        private readonly IConnectorStatusService _connectorStatusService;
        private readonly IChargePointRepository _chargePointRepository;
        private readonly Ocpp16ConnectorResolver _connectors;

        public StatusNotification16Handler(IMessageLogRepository messageLog, ILogger<StatusNotification16Handler> logger,
            IConnectorStatusService connectorStatusService, IChargePointRepository chargePointRepository, Ocpp16ConnectorResolver connectors)
            : base(messageLog, logger)
        {
            _connectorStatusService = connectorStatusService;
            _chargePointRepository = chargePointRepository;
            _connectors = connectors;
        }

        protected override async Task<Ocpp16Outcome<Empty16Response>> Process(StatusNotification16Request request, ChargePointStatus chargePointStatus)
        {
            var logResult = $"Status={request.Status}" + (request.ErrorCode is null or "NoError" ? "" : $" / ErrorCode={request.ErrorCode} {request.Info} {request.VendorErrorCode}".TrimEnd());
            if (request.ErrorCode is not (null or "NoError"))
                Logger.LogWarning("StatusNotification (1.6) => {ChargePointId} connector {ConnectorId}: {Status} error {ErrorCode} ({Info}, vendor {VendorErrorCode})",
                    chargePointStatus.Id, request.ConnectorId, request.Status, request.ErrorCode, request.Info, request.VendorErrorCode);
            else
                Logger.LogInformation("StatusNotification (1.6) => {ChargePointId} connector {ConnectorId}: {Status}", chargePointStatus.Id, request.ConnectorId, request.Status);

            var status = Ocpp16Mapping.ToConnectorStatus(request.Status);
            if (status == null)
            {
                Logger.LogWarning("StatusNotification (1.6) => Unknown status {Status} from {ChargePointId}", request.Status, chargePointStatus.Id);
                return new(new Empty16Response(), logResult, request.ConnectorId);
            }

            if (!chargePointStatus.OnlineConnectors.TryGetValue(request.ConnectorId, out var online))
            {
                online = new OnlineConnectorStatus();
                chargePointStatus.OnlineConnectors[request.ConnectorId] = online;
            }
            online.Status = status.Value;

            if (request.ConnectorId > 0)
            {
                var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);
                if (chargePoint == null)
                {
                    Logger.LogWarning("StatusNotification (1.6) => Unknown charge point {ChargePointId}", chargePointStatus.Id);
                }
                else if (await _connectors.GetOrCreateAsync(chargePoint, request.ConnectorId) is { } connector)
                {
                    var timestamp = request.Timestamp.HasValue ? Ocpp16Mapping.ToUtc(request.Timestamp.Value) : DateTime.UtcNow;
                    if (!await _connectorStatusService.UpdateConnectorStatus(connector.ConnectorID ?? 1, request.ConnectorId, status.Value, new DateTimeOffset(timestamp), chargePointStatus.Id))
                        Logger.LogError("StatusNotification (1.6) => Status of connector {ConnectorId} of {ChargePointId} not stored", request.ConnectorId, chargePointStatus.Id);
                }
            }

            return new(new Empty16Response(), logResult, request.ConnectorId);
        }
    }

    public class DataTransfer16Handler : Ocpp16HandlerBase<DataTransfer16Request, DataTransfer16Response>
    {
        public DataTransfer16Handler(IMessageLogRepository messageLog, ILogger<DataTransfer16Handler> logger) : base(messageLog, logger)
        {
        }

        // Same answer as the 2.0.1 handler: the data is recorded in the message log and acknowledged.
        protected override Task<Ocpp16Outcome<DataTransfer16Response>> Process(DataTransfer16Request request, ChargePointStatus chargePointStatus)
        {
            Logger.LogInformation("DataTransfer (1.6) => {ChargePointId} vendor {VendorId} message {MessageId}", chargePointStatus.Id, request.VendorId, request.MessageId);
            return Task.FromResult(new Ocpp16Outcome<DataTransfer16Response>(new DataTransfer16Response { Status = "Accepted" },
                $"VendorId={request.VendorId} / MessageId={request.MessageId} / Data={request.Data}"));
        }
    }

    public class DiagnosticsStatusNotification16Handler : Ocpp16HandlerBase<DiagnosticsStatusNotification16Request, Empty16Response>
    {
        private readonly ILogUploadUrlFactory _uploads;

        public DiagnosticsStatusNotification16Handler(IMessageLogRepository messageLog, ILogger<DiagnosticsStatusNotification16Handler> logger,
            ILogUploadUrlFactory uploads) : base(messageLog, logger)
        {
            _uploads = uploads;
        }

        protected override async Task<Ocpp16Outcome<Empty16Response>> Process(DiagnosticsStatusNotification16Request request, ChargePointStatus chargePointStatus)
        {
            Logger.LogInformation("DiagnosticsStatusNotification (1.6) => {ChargePointId} Status={Status}", chargePointStatus.Id, request.Status);
            await _uploads.RecordStatusAsync(chargePointStatus.Id, null, request.Status);
            return new(new Empty16Response(), request.Status);
        }
    }

    public class FirmwareStatusNotification16Handler : Ocpp16HandlerBase<FirmwareStatusNotification16Request, Empty16Response>
    {
        public FirmwareStatusNotification16Handler(IMessageLogRepository messageLog, ILogger<FirmwareStatusNotification16Handler> logger) : base(messageLog, logger)
        {
        }

        protected override Task<Ocpp16Outcome<Empty16Response>> Process(FirmwareStatusNotification16Request request, ChargePointStatus chargePointStatus)
        {
            Logger.LogInformation("FirmwareStatusNotification (1.6) => {ChargePointId} Status={Status}", chargePointStatus.Id, request.Status);
            return Task.FromResult(new Ocpp16Outcome<Empty16Response>(new Empty16Response(), request.Status));
        }
    }
}
