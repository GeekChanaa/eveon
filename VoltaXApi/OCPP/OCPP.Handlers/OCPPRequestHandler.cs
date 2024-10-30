

using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    public class OCPPRequestHandler : IOCPPRequestHandler
    {
        public const string VENDOR_ID = "VoltaX Charging";
        
        public OCPPRequestHandler()
        {
        }

        public OCPPMessage ProcessRequest(OCPPMessage msgIn)
        {
          OCPPMessage msgOut = new OCPPMessage();
          msgOut.MessageType = "3";
          msgOut.UniqueId = msgIn.UniqueId;
          msgOut.Action = msgIn.Action;

          string errorCode = null;

          if (msgIn.MessageType == "2")
          {
              switch (msgIn.Action)
              {
                  case "BootNotification":
                      errorCode = HandleBootNotification(msgIn, msgOut);
                      break;

                  case "Heartbeat":
                      errorCode = HandleHeartBeat(msgIn, msgOut);
                      break;

                  case "Authorize":
                      errorCode = HandleAuthorize(msgIn, msgOut);
                      break;

                  case "TransactionEvent":
                      errorCode = HandleTransactionEvent(msgIn, msgOut);
                      break;

                  case "MeterValues":
                      errorCode = HandleMeterValues(msgIn, msgOut);
                      break;

                  case "StatusNotification":
                      errorCode = HandleStatusNotification(msgIn, msgOut);
                      break;

                  case "DataTransfer":
                      errorCode = HandleDataTransfer(msgIn, msgOut);
                      break;

                  case "LogStatusNotification":
                      errorCode = HandleLogStatusNotification(msgIn, msgOut);
                      break;

                  case "FirmwareStatusNotification":
                      errorCode = HandleFirmwareStatusNotification(msgIn, msgOut);
                      break;

                  case "ClearedChargingLimit":
                      errorCode = HandleClearedChargingLimit(msgIn, msgOut);
                      break;

                  case "NotifyChargingLimit":
                      errorCode = HandleNotifyChargingLimit(msgIn, msgOut);
                      break;

                  case "NotifyEVChargingSchedule":
                      errorCode = HandleNotifyEVChargingSchedule(msgIn, msgOut);
                      break;

                  default:
                      errorCode = ErrorCodes.NotSupported;
                      WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, errorCode);
                      break;
              }
          }
          else
          {
              Logger.LogError("ControllerOCPP20 => Protocol error: wrong message type", msgIn.MessageType);
              errorCode = ErrorCodes.ProtocolError;
          }

          if (!string.IsNullOrEmpty(errorCode))
          {
              // Inavlid message type => return type "4" (CALLERROR)
              msgOut.MessageType = "4";
              msgOut.ErrorCode = errorCode;
              Logger.LogDebug("ControllerOCPP20 => Return error code messge: ErrorCode={0}", errorCode);
          }
          
          return msgOut;
        }
    }

}