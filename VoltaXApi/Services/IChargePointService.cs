
using Microsoft.AspNetCore.Http;
using OCPP.Core.Server;
using System;
using System.IO;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
namespace VoltaXApi.Services
{
    public interface IChargePointService
    {
      Task SetBootNotificationInfo(ChargePointStatus chargePointStatus, BootNotificationRequest bootNotificationRequest);
      Task HandleChargePointDisconnected(string chargePointID);
    }
}