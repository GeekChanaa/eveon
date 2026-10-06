
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
  public class ChargePointService : IChargePointService
  {
    private readonly IChargePointRepository _chargePointRepository;
    private readonly IChargePointModelRepository _chargePointModelRepository;
    private readonly IConnectorStatusService _connectorStatusService;
    private readonly ILogger _logger;

    public ChargePointService(
      ILoggerFactory loggerFactory,
      IChargePointRepository chargePointRepository,
      IChargePointModelRepository chargePointModelRepository,
      IConnectorStatusService connectorStatusService
    )
    {
      _chargePointRepository = chargePointRepository;
      _chargePointModelRepository = chargePointModelRepository;
      _connectorStatusService = connectorStatusService;
      _logger = loggerFactory.CreateLogger(typeof(ChargePointService));
    }

    public async Task SetBootNotificationInfo(ChargePointStatus chargePointStatus, BootNotificationRequest bootNotificationRequest)
    {
      _logger.LogTrace("Updating Informations for ChargePoint : " + chargePointStatus.Id);
      var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);
      int? chargePointModelID = await this._chargePointModelRepository.GetChargePointModelIDByIdentifier(bootNotificationRequest.ChargingStation.Model);

      if (chargePointModelID != null)
        chargePoint.ChargePointModelID = chargePointModelID;

      chargePoint.SerialNumber = bootNotificationRequest.ChargingStation.SerialNumber;
      chargePoint.VendorName = bootNotificationRequest.ChargingStation.VendorName;
      await _chargePointRepository.Update(chargePoint);
    }

    public async Task HandleChargePointDisconnected(string chargePointID)
    {
      var chargePoint = await this._chargePointRepository.GetChargePointByChargePointIDAsync(chargePointID);

      // Handling disconnected connectors 
      await HandleDisconnectedConnectors(chargePoint);
    }

    private async Task HandleDisconnectedConnectors(ChargePoint chargePoint)
    {
      List<Connector> connectors = await this._chargePointRepository.GetChargePointConnectors(chargePoint.ID);
      foreach (var connector in connectors)
      {
        await this._connectorStatusService.UpdateConnectorStatus(connector.ConnectorID ?? 0, connector.EvseID, ConnectorStatusEnumType.Disconnected, DateTime.UtcNow, chargePoint.ChargePointId);
      }
    }
    
  }
}