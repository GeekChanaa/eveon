
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
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    public class ConnectorService : IConnectorService
    {
        private readonly IConnectorRepository _connectorRepository;
        private readonly IChargePointRepository _chargePointRepository;
        private readonly GlobalConfigurations _globalConfig;

        public ConnectorService(
          IConnectorRepository connectorRepository,
          IChargePointRepository chargePointRepository,
          GlobalConfigurations globalConfigurations
        ){
          _connectorRepository = connectorRepository;
          _chargePointRepository = chargePointRepository;
          _globalConfig = globalConfigurations;
        }

      public async Task<List<ConnectorListDto>?> RefreshChargePointConnectors(List<ReportDataType>? connectorsToRefresh, string chargePointID)
      {

        var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointID);
        // Delete all connectors that are not included in the connectors to refresh as they do not belong to the charge point

        foreach(var reportData in connectorsToRefresh)
        {
          Connector? connector = await this._connectorRepository
            .GetConnectorByConnectorIdEvseId(reportData.Component.Evse.ConnectorId,reportData.Component.Evse.Id,chargePoint.ID);
          if(connector == null)
          {
            Connector newConnector = new Connector{
              ChargePointID = chargePoint.ID,
              EvseID = reportData.Component.Evse.Id,
              ConnectorID = reportData.Component.Evse.ConnectorId,
              PricePerIdleMinute = (decimal) _globalConfig.DefaultIdleTimePricing,
              PricePerKWh = (decimal) _globalConfig.DefaultPricePerKwh,
              CostPerKwh = (decimal) _globalConfig.DefaultCostPerKwh
            };
            await _connectorRepository.AddAsync(newConnector);
          }
        }

        return await this._connectorRepository.GetChargePointConnectors(chargePoint.ID);

      }


        private async Task<bool> DeleteExistingConnectorsForFirstConfiguration(int chargePointID,List<ReportDataType>? connectorsToRefresh)
        {
          if (connectorsToRefresh == null || !connectorsToRefresh.Any())
          {
              return false; // No connectors to refresh
          }

          var chargePointConnectors = await this._connectorRepository.GetChargePointConnectors(chargePointID);

          // Connectors that are not conform
          var connectorsToRemove = chargePointConnectors.Where(chargePointConnector =>
              !connectorsToRefresh.Any(connectorsToRefreshItem =>
                  connectorsToRefreshItem.Component.Evse.Id == chargePointConnector.EvseID &&
                  connectorsToRefreshItem.Component.Evse.ConnectorId == chargePointConnector.ConnectorID)).ToList();

          // Connectors that are not yet in there
          connectorsToRefresh.RemoveAll(connectorsToRefreshItem =>
            chargePointConnectors.Any(chargePointConnector =>
                connectorsToRefreshItem.Component.Evse.Id == chargePointConnector.EvseID &&
                connectorsToRefreshItem.Component.Evse.ConnectorId == chargePointConnector.ConnectorID));

          // Remove the identified connectors
          foreach (var connector in connectorsToRemove)
          {
              await this._connectorRepository.RemoveByID(connector.ID);
          }
          

          return true;
        }

  }
}