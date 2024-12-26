
using Microsoft.AspNetCore.Http;
using OCPP.Core.Server;
using System;
using System.IO;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
namespace VoltaXApi.Services
{
    public interface IConnectorService
    {
      Task<List<ConnectorListDto>?> RefreshChargePointConnectors(List<ReportDataType>? connectorsToRefresh, string chargePointID);
    }
}