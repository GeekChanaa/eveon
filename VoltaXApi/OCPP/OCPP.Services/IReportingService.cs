using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  public interface IReportingService
  {
    Task GetBaseReport(string chargePointID, GetBaseReportRequest request);
    Task GetReport(string chargePointID, GetReportRequest request);
    Task GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request);
    Task GetLog(string chargePointID, GetLogRequest request);
    Task CustomerInformation(string chargePointID, CustomerInformationRequest request);
  }
}