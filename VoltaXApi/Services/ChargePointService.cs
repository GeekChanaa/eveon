
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
        private readonly ILogger _logger;

        public ChargePointService( 
          ILoggerFactory loggerFactory,
          IChargePointRepository chargePointRepository,
          IChargePointModelRepository chargePointModelRepository
        ){
          _chargePointRepository = chargePointRepository;
          _chargePointModelRepository = chargePointModelRepository;
          _logger = loggerFactory.CreateLogger(typeof(ChargePointService));
        }

    public async Task SetBootNotificationInfo(ChargePointStatus chargePointStatus, BootNotificationRequest bootNotificationRequest)
    {
      _logger.LogTrace("Updating Informations for ChargePoint : " + chargePointStatus.Id);
      var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);
      chargePoint.Model = bootNotificationRequest.ChargingStation.Model;
      int? chargePointModelID = await this._chargePointModelRepository.GetChargePointModelIDByIdentifier(chargePoint.Model);

      if(chargePointModelID != null)
        chargePoint.ChargePointModelID = chargePointModelID;
      chargePoint.SerialNumber = bootNotificationRequest.ChargingStation.SerialNumber;
      chargePoint.VendorName = bootNotificationRequest.ChargingStation.VendorName;
      await _chargePointRepository.Update(chargePoint);
    }
  }
}