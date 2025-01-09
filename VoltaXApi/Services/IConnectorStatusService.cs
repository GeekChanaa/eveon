
using Microsoft.AspNetCore.Http;
using OCPP.Core.Server;
using System;
using System.IO;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
namespace VoltaXApi.Services
{
    public interface IConnectorStatusService
    {
      Task<bool> RefreshConnectorStatuses(List<ReportDataType>? ReportData, string chargePointID);
      Task<bool> UpdateConnectorStatus(int connectorId, int evseId, ConnectorStatusEnumType status, DateTimeOffset? statusTime, ChargePointStatus chargePointStatus);
    }
}