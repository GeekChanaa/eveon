using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>Reporting commands. Every command waits for the charger's answer (see <see cref="IOcppCommandSender"/>).</summary>
    public class ReportingService : IReportingService
    {
        private readonly IOcppCommandSender _commandSender;
        private readonly ILogUploadUrlFactory _logUploadUrls;
        private readonly IOcppDeviceDataService _deviceData;
        private readonly MonitoringReportAssembler _monitoringReports;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(IOcppCommandSender commandSender, ILogUploadUrlFactory logUploadUrls, IOcppDeviceDataService deviceData,
            MonitoringReportAssembler monitoringReports, ILogger<ReportingService> logger)
        {
            _commandSender = commandSender;
            _logUploadUrls = logUploadUrls;
            _deviceData = deviceData;
            _monitoringReports = monitoringReports;
            _logger = logger;
        }

        public async Task<GetBaseReportResponse> GetBaseReport(string chargePointID, GetBaseReportRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<GetBaseReportRequest, GetBaseReportResponse>(chargePointID, "GetBaseReport", request, cancellationToken: cancellationToken);
            _logger.LogInformation("GetBaseReport answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<GetReportResponse> GetReport(string chargePointID, GetReportRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<GetReportRequest, GetReportResponse>(chargePointID, "GetReport", request, cancellationToken: cancellationToken);
            _logger.LogInformation("GetReport answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<GetMonitoringReportResponse> GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request, CancellationToken cancellationToken = default)
        {
            // Only an unfiltered report describes every monitor of the charger, so only it replaces the stored ones.
            var fullReport = request.ComponentVariable is not { Count: > 0 } && request.MonitoringCriteria is not { Count: > 0 };
            if (fullReport) _monitoringReports.MarkFullReport(chargePointID, request.RequestId);
            var response = await _commandSender.SendRequestAsync<GetMonitoringReportRequest, GetMonitoringReportResponse>(chargePointID, "GetMonitoringReport", request, cancellationToken: cancellationToken);
            _logger.LogInformation("GetMonitoringReport {RequestId} answered {Status} by {ChargePointId}", request.RequestId, response.Status, chargePointID);
            if (fullReport && response.Status == GenericDeviceModelStatusEnumType.EmptyResultSet)
                await _deviceData.ApplyMonitoringReportAsync(chargePointID, request.RequestId, Array.Empty<MonitoringDataType>(), replaceAll: true, cancellationToken);
            return response;
        }

        /// <summary>
        /// Without a remoteLocation, the charger is given a one-time upload URL of this CSMS (see <see cref="ILogUploadUrlFactory"/>);
        /// the uploaded file is then listed under api/logs/{chargePointID}.
        /// </summary>
        public async Task<GetLogResponse> GetLog(string chargePointID, GetLogRequest request, CancellationToken cancellationToken = default)
        {
            request.Log ??= new LogParametersType();
            if (string.IsNullOrWhiteSpace(request.Log.RemoteLocation))
            {
                var purpose = request.LogType == LogEnumType.SecurityLog ? LogUploadPurposes.SecurityLog : LogUploadPurposes.DiagnosticsLog;
                var upload = await _logUploadUrls.CreateAsync(chargePointID, request.RequestId, purpose, cancellationToken);
                request.Log.RemoteLocation = upload.Url;
            }
            var response = await _commandSender.SendRequestAsync<GetLogRequest, GetLogResponse>(chargePointID, "GetLog", request, cancellationToken: cancellationToken);
            _logger.LogInformation("GetLog {RequestId} answered {Status} by {ChargePointId}", request.RequestId, response.Status, chargePointID);
            await _logUploadUrls.RecordStatusAsync(chargePointID, request.RequestId, response.Status.ToString(), response.Filename, cancellationToken);
            return response;
        }

        public async Task<CustomerInformationResponse> CustomerInformation(string chargePointID, CustomerInformationRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<CustomerInformationRequest, CustomerInformationResponse>(chargePointID, "CustomerInformation", request, cancellationToken: cancellationToken);
            _logger.LogInformation("CustomerInformation {RequestId} answered {Status} by {ChargePointId}", request.RequestId, response.Status, chargePointID);
            await _deviceData.RecordCustomerInformationRequestAsync(chargePointID, request, response.Status.ToString(), cancellationToken);
            return response;
        }
    }
}
