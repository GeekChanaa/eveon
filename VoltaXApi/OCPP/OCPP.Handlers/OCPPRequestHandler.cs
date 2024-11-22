

using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    public class OCPPRequestHandler
    {   
        public const string VENDOR_ID = "VoltaX Charging";
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ILogger _logger;
        private readonly OCPPRequestHandlerFactory _handlerFactory;
        
        public OCPPRequestHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            OCPPRequestHandlerFactory handlerFactory
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(LogStatusNotificationHandler));
            _msgLogRepo = messageLogRepository;
            _handlerFactory = handlerFactory;
        }

        public async Task<OCPPMessage> ProcessRequest(OCPPMessage msgIn, ChargePointStatus chargePointStatus)
        {
            OCPPMessage msgOut = new OCPPMessage
            {
                MessageType = "3",
                UniqueId = msgIn.UniqueId,
                Action = msgIn.Action
            };

            

            if (msgIn.MessageType == "2")
            {
                var handler = _handlerFactory.GetHandler(msgIn.Action);

                if (handler != null)
                {
                    string errorCode = await handler.Handle(msgIn, msgOut, chargePointStatus);
                    
                    
                    if (!string.IsNullOrEmpty(errorCode))
                    {
                        msgOut.MessageType = "4"; // Error type
                        msgOut.ErrorCode = errorCode;
                        _logger.LogDebug("ControllerOCPP20 => Return error code message: ErrorCode={0}", errorCode);
                    }
                }
                else
                {
                    Console.WriteLine("No handler for this action");
                    // Log unsupported action
                    string errorCode = ErrorCodes.NotSupported;
                    await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, errorCode, msgIn, msgOut);
                    msgOut.MessageType = "4";
                    msgOut.ErrorCode = errorCode;
                }
            }
            else
            {
                _logger.LogError("ControllerOCPP20 => Protocol error: wrong message type", msgIn.MessageType);
                msgOut.MessageType = "4";
                msgOut.ErrorCode = ErrorCodes.ProtocolError;
            }

            return msgOut;
        }
    }

}