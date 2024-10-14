

using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  public interface IMonitoringService
  {
    Task SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request);
    Task ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request);
    Task SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request);
    Task SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request);
    Task SetVariables(string chargePointID, SetVariablesRequest request);
    Task GetVariables(string chargePointID, GetVariablesRequest request);
  }
}