using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    public class StatusNotificationHandler : IOCPPRequestHandler
    {

        private readonly IMessageLogRepository _msgLogRepo;
        private readonly IConnectorStatusService _connectorStatusService;
        private readonly ILogger _logger;
        public StatusNotificationHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            IConnectorStatusService connectorStatusService
        )
        {
            _msgLogRepo = messageLogRepository;
            _logger = loggerFactory.CreateLogger(typeof(StatusNotificationHandler));
            _connectorStatusService = connectorStatusService;
        }
        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            StatusNotificationResponse statusNotificationResponse = new StatusNotificationResponse();

            statusNotificationResponse.CustomData = new CustomDataType();
            statusNotificationResponse.CustomData.VendorId = OCPPHelper.VendorId;

            int connectorId = -1;
            int evseId = 0;
            bool msgWritten = false;

            try
            {
                _logger.LogTrace("Processing status notification...");
                StatusNotificationRequest statusNotificationRequest = JsonConvert.DeserializeObject<StatusNotificationRequest>(msgIn.JsonPayload);
                _logger.LogTrace("StatusNotification => Message deserialized");

                connectorId = statusNotificationRequest.ConnectorId;
                evseId = statusNotificationRequest.EvseId;

                // Write raw status in DB
                msgWritten = await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, string.Format("Status={0}", statusNotificationRequest.ConnectorStatus), string.Empty, msgIn, msgOut);
                ConnectorStatusEnumType newStatus = statusNotificationRequest.ConnectorStatus;

                _logger.LogInformation("StatusNotification => ChargePoint={0} / Connector={1}  / newStatus={2} / EvseID={3}", chargePointStatus?.Id, connectorId, newStatus.ToString(), evseId);

                if (connectorId >= 0)
                {
                    if (await _connectorStatusService.UpdateConnectorStatus(connectorId,evseId, newStatus, DateTimeOffset.Parse(statusNotificationRequest.Timestamp), chargePointStatus) == false)
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
                await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, null, errorCode, msgIn, msgOut);
            }
            return errorCode;
        }

        
    }
}