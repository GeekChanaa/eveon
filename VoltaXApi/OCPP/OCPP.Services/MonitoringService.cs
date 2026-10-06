using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>Monitoring and variable commands. Every command waits for the charger's answer (see <see cref="IOcppCommandSender"/>).</summary>
    public class MonitoringService : IMonitoringService
    {
        private readonly IOcppCommandSender _commandSender;
        private readonly IOcppDeviceDataService _deviceData;
        private readonly ILogger<MonitoringService> _logger;

        public MonitoringService(IOcppCommandSender commandSender, IOcppDeviceDataService deviceData, ILogger<MonitoringService> logger)
        {
            _commandSender = commandSender;
            _deviceData = deviceData;
            _logger = logger;
        }

        public async Task<SetVariableMonitoringResponse> SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<SetVariableMonitoringRequest, SetVariableMonitoringResponse>(chargePointID, "SetVariableMonitoring", request, cancellationToken: cancellationToken);
            _logger.LogInformation("SetVariableMonitoring answered by {ChargePointId}", chargePointID);
            // Keeps the stored monitors (severity of incoming events) in line with what the charger accepted.
            await _deviceData.ApplySetMonitoringResultAsync(chargePointID, request, response, cancellationToken);
            return response;
        }

        public async Task<ClearVariableMonitoringResponse> ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<ClearVariableMonitoringRequest, ClearVariableMonitoringResponse>(chargePointID, "ClearVariableMonitoring", request, cancellationToken: cancellationToken);
            _logger.LogInformation("ClearVariableMonitoring answered by {ChargePointId}", chargePointID);
            await _deviceData.RemoveMonitorsAsync(chargePointID,
                (response.ClearMonitoringResult ?? new List<ClearMonitoringResultType>())
                    .Where(r => r != null && r.Status is ClearMonitoringStatusEnumType.Accepted or ClearMonitoringStatusEnumType.NotFound)
                    .Select(r => r.Id), cancellationToken);
            return response;
        }

        public async Task<SetMonitoringLevelResponse> SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<SetMonitoringLevelRequest, SetMonitoringLevelResponse>(chargePointID, "SetMonitoringLevel", request, cancellationToken: cancellationToken);
            _logger.LogInformation("SetMonitoringLevel answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<SetMonitoringBaseResponse> SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<SetMonitoringBaseRequest, SetMonitoringBaseResponse>(chargePointID, "SetMonitoringBase", request, cancellationToken: cancellationToken);
            _logger.LogInformation("SetMonitoringBase answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<SetVariablesResponse> SetVariables(string chargePointID, SetVariablesRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<SetVariablesRequest, SetVariablesResponse>(chargePointID, "SetVariables", request, cancellationToken: cancellationToken);
            _logger.LogInformation("SetVariables answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<GetVariablesResponse> GetVariables(string chargePointID, GetVariablesRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _commandSender.SendRequestAsync<GetVariablesRequest, GetVariablesResponse>(chargePointID, "GetVariables", request, cancellationToken: cancellationToken);
            _logger.LogInformation("GetVariables answered by {ChargePointId}", chargePointID);
            return response;
        }
    }
}
