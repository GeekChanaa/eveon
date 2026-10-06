using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>Upload progress of a GetLog request: recorded on its upload ticket so the dashboard shows it.</summary>
    public class LogStatusNotificationHandler : DeviceDataNotificationHandler<LogStatusNotificationRequest, LogStatusNotificationResponse>
    {
        private readonly ILogUploadUrlFactory _logUploads;

        public LogStatusNotificationHandler(IMessageLogRepository messageLogRepository, ILogUploadUrlFactory logUploads, ILogger<LogStatusNotificationHandler> logger)
            : base(messageLogRepository, logger) => _logUploads = logUploads;

        protected override async Task<string?> Process(string chargePointId, LogStatusNotificationRequest request)
        {
            var status = request.Status.ToString();
            Logger.LogInformation("LogStatusNotification => {ChargePointId} request {RequestId} Status={Status}", chargePointId, request.RequestId, status);
            // requestId is absent only for the Idle status (no upload in progress).
            if (request.RequestId.HasValue)
                await _logUploads.RecordStatusAsync(chargePointId, request.RequestId, status);
            return status;
        }
    }
}
