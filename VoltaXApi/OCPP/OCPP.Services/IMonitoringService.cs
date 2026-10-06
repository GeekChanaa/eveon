using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  public interface IMonitoringService
  {
    Task<SetVariableMonitoringResponse> SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request, CancellationToken cancellationToken = default);
    Task<ClearVariableMonitoringResponse> ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request, CancellationToken cancellationToken = default);
    Task<SetMonitoringLevelResponse> SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request, CancellationToken cancellationToken = default);
    Task<SetMonitoringBaseResponse> SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request, CancellationToken cancellationToken = default);
    Task<SetVariablesResponse> SetVariables(string chargePointID, SetVariablesRequest request, CancellationToken cancellationToken = default);
    Task<GetVariablesResponse> GetVariables(string chargePointID, GetVariablesRequest request, CancellationToken cancellationToken = default);
  }
}
