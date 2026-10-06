using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Helpers;
using VoltaXApi.OCPP.Helpers;

namespace VoltaXApi.Services
{
  public class ConnectorStatusService : IConnectorStatusService
  {
    private readonly IConnectorStatusRepository _connectorStatusRepository;
    private readonly IConnectorRepository _connectorRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChargePointRepository _chargePointRepository;
    private readonly IConnectorUptimeRepository _connectorUptimeRepository;
    private readonly ISystemReportService _systemReportService;
    private readonly IChargingSessionService _chargingSessionService;
    private readonly ILogger<ConnectorStatusService> _logger;

    public ConnectorStatusService(
      IConnectorStatusRepository csrepo,
      IConnectorRepository connectorRepository,
      IServiceScopeFactory scopeFactory,
      IChargePointRepository chargePointRepository,
      IConnectorUptimeRepository connectorUptimeRepository,
      ISystemReportService systemReportService,
      IChargingSessionService chargingSessionService,
      ILogger<ConnectorStatusService> logger
    )
    {
      _connectorStatusRepository = csrepo;
      _connectorRepository = connectorRepository;
      _scopeFactory = scopeFactory;
      _chargePointRepository = chargePointRepository;
      _connectorUptimeRepository = connectorUptimeRepository;
      _systemReportService = systemReportService;
      _chargingSessionService = chargingSessionService;
      _logger = logger;
    }

    public async Task<bool> RefreshConnectorStatuses(List<ReportDataType>? ReportData, string chargePointID)
    {
      if (ReportData == null || ReportData.Count == 0)
        return true;

      try
      {
        ChargePoint? chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointID);
        if (chargePoint == null)
        {
          _logger.LogWarning("RefreshConnectorStatuses => Unknown charge point {ChargePointId}", chargePointID);
          return false;
        }

        foreach (var connectorStatusData in ReportData)
        {
          var evse = connectorStatusData.Component?.Evse;
          var actual = connectorStatusData.VariableAttribute?.FirstOrDefault(a => a.Type == AttributeEnumType.Actual);
          if (evse == null || actual == null)
            continue;

          Connector? connector = await _connectorRepository.GetConnectorByConnectorIdEvseId(evse.ConnectorId, evse.Id, chargePoint.ID);
          if (connector == null)
          {
            _logger.LogWarning("RefreshConnectorStatuses => Connector {EvseId}/{ConnectorId} of {ChargePointId} not found", evse.Id, evse.ConnectorId, chargePointID);
            continue;
          }

          ConnectorStatus? connectorStatus = await _connectorStatusRepository.GetConnectorStatusByConnectorID(connector.ID);
          if (connectorStatus == null)
          {
            connectorStatus = new ConnectorStatus
            {
              ConnectorID = connector.ID,
              LastStatus = ConnectorStatusHelper.ConvertToEnum(actual.Value),
              LastStatusTime = DateTime.UtcNow
            };
            _logger.LogDebug("RefreshConnectorStatuses => Creating connector status for connector {ConnectorId}", connector.ID);
            await _connectorStatusRepository.AddAsync(connectorStatus);
          }
          else if (!string.IsNullOrEmpty(actual.Value))
          {
            connectorStatus.LastStatus = ConnectorStatusHelper.ConvertToEnum(actual.Value);
            connectorStatus.LastStatusTime = DateTime.UtcNow;
            await _connectorStatusRepository.Update(connectorStatus);
          }
        }
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "RefreshConnectorStatuses => Failed for charge point {ChargePointId}", chargePointID);
        return false;
      }

      return true;
    }

    public async Task<bool> UpdateConnectorStatus(int connectorId, int evseId, ConnectorStatusEnumType status, DateTimeOffset? statusTime, string chargePointID)
    {
      try
      {
        ChargePoint? chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointID);
        if (chargePoint == null)
        {
          _logger.LogWarning("UpdateConnectorStatus => Unknown charge point {ChargePointId}", chargePointID);
          return false;
        }

        Connector? connector = await _connectorRepository.GetConnectorByConnectorIdEvseId(connectorId, evseId, chargePoint.ID);
        if (connector == null)
        {
          // Unknown connector: ask for the device model without blocking the charger's StatusNotification.
          OcppBackgroundCommand.Run(_scopeFactory, _logger, "RefreshConnectors", chargePointID,
              services => services.GetRequiredService<IConfigurationService>().RefreshConnectors(chargePointID));
          return true;
        }

        ConnectorStatus? connectorStatus = await _connectorStatusRepository.GetConnectorStatusByConnectorID(connector.ID);
        if (status == ConnectorStatusEnumType.Faulted)
        {
          SystemReport sysReport = new()
          {
            ReportCategory = ReportCategoryEnum.Technical,
            ConnectorID = connector.ID,
            IssueDescription = "Connector Faulted",
            IsEmail = true,
            IsNotification = true,
            Criticality = ReportCriticality.High
          };
          await _systemReportService.HandleReport(sysReport);
        }
        if (connectorStatus != null && status == ConnectorStatusEnumType.Available && connectorStatus.LastStatus == ConnectorStatusEnumType.Occupied)
        {
          await _chargingSessionService.HandleIdleMinutes(connector.ID, statusTime);
        }

        if (connectorStatus == null)
        {
          connectorStatus = new ConnectorStatus
          {
            ConnectorID = connector.ID,
            LastStatus = status
          };
          _logger.LogDebug("UpdateConnectorStatus => Creating connector status for connector {ConnectorId}", connector.ID);
          await _connectorStatusRepository.AddAsync(connectorStatus);
        }
        else
        {
          connectorStatus.LastStatus = status;
          connectorStatus.LastStatusTime = (statusTime ?? DateTimeOffset.UtcNow).UtcDateTime;
          await _connectorStatusRepository.Update(connectorStatus);
        }

        await _connectorUptimeRepository.UpdateConnectorUptime(connector.ID, status);
        _logger.LogDebug("UpdateConnectorStatus => {ChargePointId} EVSE {EvseId} connector {ConnectorId}: {Status}", chargePointID, evseId, connectorId, status);
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "UpdateConnectorStatus => Failed for {ChargePointId} EVSE {EvseId} connector {ConnectorId}", chargePointID, evseId, connectorId);
        return false;
      }

      return true;
    }

    public async Task<bool> IsEVCableConnected(string chargePointID)
    {
      return await _connectorStatusRepository.IsEVCableConnected(chargePointID);
    }
  }
}
