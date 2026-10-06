using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  public interface IReportingService
  {
    Task<GetBaseReportResponse> GetBaseReport(string chargePointID, GetBaseReportRequest request, CancellationToken cancellationToken = default);
    Task<GetReportResponse> GetReport(string chargePointID, GetReportRequest request, CancellationToken cancellationToken = default);
    Task<GetMonitoringReportResponse> GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request, CancellationToken cancellationToken = default);
    Task<GetLogResponse> GetLog(string chargePointID, GetLogRequest request, CancellationToken cancellationToken = default);
    Task<CustomerInformationResponse> CustomerInformation(string chargePointID, CustomerInformationRequest request, CancellationToken cancellationToken = default);
  }
}
