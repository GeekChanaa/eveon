
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.Models;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Helpers;

namespace VoltaXApi.Services
{
  public class ConnectorStatusService : IConnectorStatusService
  {
    private readonly IConnectorStatusRepository _connectorStatusRepository;
    private readonly IConnectorService _connectorService;
    private readonly IConnectorRepository _connectorRepository;
    private readonly IConfigurationService _configurationService;
    private readonly IChargePointRepository _chargePointRepository;
    private readonly IConnectorUptimeRepository _connectorUptimeRepository;
    private readonly ISystemReportService _systemReportService;
    private readonly IChargingSessionRepository _chargingSessionRepository;
    private readonly IChargingSessionService _chargingSessionService;

    public ConnectorStatusService(
      IConnectorStatusRepository csrepo,
      IConnectorRepository connectorRepository,
      IConnectorService connectorService,
      IConfigurationService configurationService,
      IChargePointRepository chargePointRepository,
      IConnectorUptimeRepository connectorUptimeRepository,
      ISystemReportService systemReportService,
      IChargingSessionService chargingSessionService
    )
    {
      _connectorStatusRepository = csrepo;
      _connectorStatusRepository = csrepo;
      _connectorService = connectorService;
      _connectorRepository = connectorRepository;
      _configurationService = configurationService;
      _chargePointRepository = chargePointRepository;
      _connectorUptimeRepository = connectorUptimeRepository;
      _systemReportService = systemReportService;
      _chargingSessionService = chargingSessionService;
    }

    public async Task<bool> RefreshConnectorStatuses(List<ReportDataType>? ReportData, string chargePointID)
    {
      try
      {
        foreach (var connectorStatusData in ReportData)
        {
          ChargePoint? chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointID);
          Connector? connector = await _connectorRepository.GetConnectorByConnectorIdEvseId(connectorStatusData.Component.Evse.ConnectorId, connectorStatusData.Component.Evse.Id, chargePoint.ID);
          ConnectorStatus? connectorStatus = await _connectorStatusRepository.GetConnectorStatusByConnectorID(connector.ID);
          if (connectorStatus == null)
          {
            // no matching entry => create connector status
            connectorStatus = new ConnectorStatus
            {
              ConnectorID = connector.ID,
              LastStatus = ConnectorStatusHelper.ConvertToEnum(connectorStatusData.VariableAttribute[0].Value),
              LastStatusTime = DateTime.Now
            };
            Console.WriteLine("Refresh Connector Statuses => Creating new DB-ConnectorStatus: Connector={0}", connectorStatus.ConnectorID);
            await _connectorStatusRepository.AddAsync(connectorStatus);
          }
          else
          {
            if (!string.IsNullOrEmpty(connectorStatusData.VariableAttribute[0].Value))
            {
              connectorStatus.LastStatus = ConnectorStatusHelper.ConvertToEnum(connectorStatusData.VariableAttribute[0].Value);
              connectorStatus.LastStatusTime = DateTime.Now;
              await _connectorStatusRepository.Update(connectorStatus);
            }
          }


        }

      }
      catch (Exception exp)
      {
        Console.WriteLine("INNER EXCEPTION : ");
        Console.WriteLine(exp.StackTrace);
        if (exp.InnerException != null)
          Console.WriteLine(exp.InnerException.ToString());
        return false;
      }

      return true;
    }

    public async Task<bool> UpdateConnectorStatus(int connectorId, int evseId, ConnectorStatusEnumType status, DateTimeOffset? statusTime, string chargePointID)
    {
      try
      {
        ChargePoint? chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointID);
        Connector? connector = await _connectorRepository.GetConnectorByConnectorIdEvseId(connectorId, evseId, chargePoint.ID);

        if (connector == null)
        {
          await this._configurationService.RefreshConnectors(chargePointID);
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
        if (status == ConnectorStatusEnumType.Available && connectorStatus.LastStatus == ConnectorStatusEnumType.Occupied)
        {
          await _chargingSessionService.HandleIdleMinutes(connector.ID, statusTime);
        }

        if (connectorStatus == null)
        {
          // no matching entry => create connector status
          connectorStatus = new ConnectorStatus
          {
            ConnectorID = connector.ID,
            LastStatus = status
          };
          Console.WriteLine("UpdateConnectorStatus => Creating new DB-ConnectorStatus:  Connector={0}", connectorStatus.ConnectorID);
          await _connectorStatusRepository.AddAsync(connectorStatus);
          await _connectorUptimeRepository.UpdateConnectorUptime(connector.ID, status);
        }
        else
        {
          connectorStatus.LastStatus = status;
          connectorStatus.LastStatusTime = ((statusTime.HasValue) ? statusTime.Value : DateTimeOffset.UtcNow).DateTime;
          await _connectorStatusRepository.Update(connectorStatus);

          // Updating uptimeReport
          await _connectorUptimeRepository.UpdateConnectorUptime(connector.ID, status);
        }

        Console.WriteLine("UpdateConnectorStatus => Save ConnectorStatus: Connector={0} / Status={1}", connectorId, status);

      }
      catch (Exception exp)
      {
        Console.WriteLine("UpdateConnectorStatus => Exception writing connector status (ID={0} / Connector={1}): {2}", chargePointID, connectorId, exp.Message);
        Console.WriteLine("INNER EXCEPTION : ");
        Console.WriteLine(exp.StackTrace);
        if (exp.InnerException != null)
          Console.WriteLine(exp.InnerException.ToString());
        return false;
      }

      return true;
    }
  }
  


}