using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    public class StatusNotificationHandler : IOCPPRequestHandler
    {

        private readonly IMessageLogRepository _msgLogRepo;
        private readonly IConnectorStatusRepository _connectorStatusRepo;
        private readonly ILogger _logger;
        public StatusNotificationHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            IConnectorStatusRepository connectorStatusRepository
        )
        {
            _msgLogRepo = messageLogRepository;
            _logger = loggerFactory.CreateLogger(typeof(StatusNotificationHandler));
            _connectorStatusRepo = connectorStatusRepository;
        }
        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            Console.WriteLine("this is the handle status ntification");
            string? errorCode = null;
            StatusNotificationResponse statusNotificationResponse = new StatusNotificationResponse();

            statusNotificationResponse.CustomData = new CustomDataType();
            statusNotificationResponse.CustomData.VendorId = OCPPHelper.VendorId;

            int connectorId = 0;
            bool msgWritten = false;

            try
            {
                _logger.LogTrace("Processing status notification...");
                StatusNotificationRequest statusNotificationRequest = JsonConvert.DeserializeObject<StatusNotificationRequest>(msgIn.JsonPayload);
                _logger.LogTrace("StatusNotification => Message deserialized");

                connectorId = statusNotificationRequest.ConnectorId;

                // Write raw status in DB
                msgWritten = await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, string.Format("Status={0}", statusNotificationRequest.ConnectorStatus), string.Empty);

                ConnectorStatusEnumType newStatus = statusNotificationRequest.ConnectorStatus;

                _logger.LogInformation("StatusNotification => ChargePoint={0} / Connector={1} / newStatus={2}", chargePointStatus?.Id, connectorId, newStatus.ToString());

                if (connectorId > 0)
                {
                    if (await _connectorStatusRepo.UpdateConnectorStatus(connectorId, newStatus.ToString(), DateTimeOffset.Parse(statusNotificationRequest.Timestamp), chargePointStatus) == false)
                    {
                        errorCode = ErrorCodes.InternalError;
                    }

                    if (chargePointStatus.OnlineConnectors.ContainsKey(connectorId))
                    {
                        OnlineConnectorStatus ocs = chargePointStatus.OnlineConnectors[connectorId];
                        ocs.Status = newStatus;
                    }
                    else
                    {
                        OnlineConnectorStatus ocs = new OnlineConnectorStatus();
                        ocs.Status = newStatus;
                        if (chargePointStatus.OnlineConnectors.TryAdd(connectorId, ocs))
                        {
                            _logger.LogTrace("StatusNotification => new OnlineConnectorStatus with values: ChargePoint={0} / Connector={1} / newStatus={2}", chargePointStatus?.Id, connectorId, newStatus.ToString());
                        }
                        else
                        {
                            _logger.LogError("StatusNotification => Error adding new OnlineConnectorStatus for ChargePoint={0} / Connector={1}", chargePointStatus?.Id, connectorId);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("StatusNotification => Status for unexpected ConnectorId={1} on ChargePoint={0}", chargePointStatus?.Id, connectorId);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(statusNotificationResponse);
                _logger.LogTrace("StatusNotification => Response serialized");
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "StatusNotification => ChargePoint={0} / Exception: {1}", chargePointStatus.Id, exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            if (!msgWritten)
            {
                await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, null, errorCode);
            }
            return errorCode;
        }

        
    }
}