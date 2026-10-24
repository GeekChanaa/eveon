using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>
    /// Shared flow of the OCPP 2.0.1 device-data notifications: deserialize, process, answer with an empty response
    /// (CustomData only), log the exchange. A payload that cannot be read is answered with FormatViolation, a processing
    /// failure with InternalError.
    /// </summary>
    public abstract class DeviceDataNotificationHandler<TRequest, TResponse> : IOCPPRequestHandler
        where TResponse : new()
    {
        private readonly IMessageLogRepository _msgLogRepo;
        protected readonly ILogger Logger;

        protected DeviceDataNotificationHandler(IMessageLogRepository msgLogRepo, ILogger logger)
        {
            _msgLogRepo = msgLogRepo;
            Logger = logger;
        }

        protected abstract Task<string?> Process(string chargePointId, TRequest request);

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            string? result = null;
            if (chargePointStatus == null) return ErrorCodes.GenericError;
            TRequest? request;
            try
            {
                request = JsonConvert.DeserializeObject<TRequest>(msgIn.JsonPayload);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "{Action} => unreadable payload from {ChargePointId}", msgIn.Action, chargePointStatus.Id);
                request = default;
            }

            if (request == null) errorCode = ErrorCodes.FormationViolation;
            else
            {
                try
                {
                    result = await Process(chargePointStatus.Id, request);
                    var response = new TResponse();
                    msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
                }
                catch (ArgumentException ex)
                {
                    Logger.LogWarning(ex, "{Action} => invalid content from {ChargePointId}", msgIn.Action, chargePointStatus.Id);
                    errorCode = ErrorCodes.PropertyConstraintViolation;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "{Action} => processing failed for {ChargePointId}", msgIn.Action, chargePointStatus.Id);
                    errorCode = ErrorCodes.InternalError;
                }
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, result!, errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }

    public class NotifyEventHandler : DeviceDataNotificationHandler<NotifyEventRequest, NotifyEventResponse>
    {
        private readonly IOcppDeviceDataService _deviceData;
        private readonly ChargerAlarmNotifier _alarms;
        private readonly VoltaXApi.Services.Alarms.IAlarmService _alarmService;

        public NotifyEventHandler(IMessageLogRepository msgLogRepo, IOcppDeviceDataService deviceData, ChargerAlarmNotifier alarms,
            VoltaXApi.Services.Alarms.IAlarmService alarmService, ILogger<NotifyEventHandler> logger)
            : base(msgLogRepo, logger)
        {
            _deviceData = deviceData;
            _alarms = alarms;
            _alarmService = alarmService;
        }

        protected override async Task<string?> Process(string chargePointId, NotifyEventRequest request)
        {
            // Every part is self-contained (tbc / seqNo only split a long list): each is stored as it arrives.
            var events = await _deviceData.RecordEventsAsync(chargePointId, request);
            await _alarms.NotifyAsync(events);
            // Alarm-worthy events raise (or count) an alarm; cleared ones close it.
            await _alarmService.OnEventsAsync(chargePointId, events);
            return $"{events.Count} event(s)";
        }
    }

    public class NotifyMonitoringReportHandler : DeviceDataNotificationHandler<NotifyMonitoringReportRequest, NotifyMonitoringReportResponse>
    {
        private readonly IOcppDeviceDataService _deviceData;
        private readonly MonitoringReportAssembler _assembler;

        public NotifyMonitoringReportHandler(IMessageLogRepository msgLogRepo, IOcppDeviceDataService deviceData, MonitoringReportAssembler assembler,
            ILogger<NotifyMonitoringReportHandler> logger) : base(msgLogRepo, logger)
        {
            _deviceData = deviceData;
            _assembler = assembler;
        }

        protected override async Task<string?> Process(string chargePointId, NotifyMonitoringReportRequest request)
        {
            var monitors = _assembler.Add(chargePointId, request);
            if (monitors == null) return $"Part {request.SeqNo ?? 0} of request {request.RequestId}";
            await _deviceData.ApplyMonitoringReportAsync(chargePointId, request.RequestId, monitors,
                _assembler.IsFullReport(chargePointId, request.RequestId));
            return $"Report {request.RequestId} complete";
        }
    }

    public class NotifyCustomerInformationHandler : DeviceDataNotificationHandler<NotifyCustomerInformationRequest, NotifyCustomerInformationResponse>
    {
        private readonly IOcppDeviceDataService _deviceData;

        public NotifyCustomerInformationHandler(IMessageLogRepository msgLogRepo, IOcppDeviceDataService deviceData, ILogger<NotifyCustomerInformationHandler> logger)
            : base(msgLogRepo, logger) => _deviceData = deviceData;

        protected override async Task<string?> Process(string chargePointId, NotifyCustomerInformationRequest request)
        {
            var report = await _deviceData.AppendCustomerInformationAsync(chargePointId, request);
            // The data itself is personal: it stays out of the message log result.
            return report.Complete ? $"Request {request.RequestId} complete" : $"Part {request.SeqNo} of request {request.RequestId}";
        }
    }

    public class NotifyDisplayMessagesHandler : DeviceDataNotificationHandler<NotifyDisplayMessagesRequestType, NotifyDisplayMessagesResponseType>
    {
        private readonly IOcppDeviceDataService _deviceData;

        public NotifyDisplayMessagesHandler(IMessageLogRepository msgLogRepo, IOcppDeviceDataService deviceData, ILogger<NotifyDisplayMessagesHandler> logger)
            : base(msgLogRepo, logger) => _deviceData = deviceData;

        protected override async Task<string?> Process(string chargePointId, NotifyDisplayMessagesRequestType request)
        {
            // requestId is required by the 2.0.1 schema; tbc parts of one request accumulate under the same id.
            if (request.RequestId == null) throw new ArgumentException("NotifyDisplayMessages without requestId.");
            var messages = (request.MessageInfo ?? new List<MessageInfoType>()).Where(m => m != null).ToList();
            await _deviceData.RecordDisplayMessagesAsync(chargePointId, request.RequestId.Value, messages);
            return $"{messages.Count} message(s)";
        }
    }
}
