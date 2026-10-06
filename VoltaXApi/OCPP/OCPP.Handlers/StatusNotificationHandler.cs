using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
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

                _logger.LogInformation("StatusNotification => {ChargePointId} EVSE {EvseId} connector {ConnectorId}: {Status}", chargePointStatus?.Id, evseId, connectorId, newStatus);

                if (connectorId >= 0)
                {
                    if (await _connectorStatusService.UpdateConnectorStatus(connectorId,evseId, newStatus, DateTimeOffset.Parse(statusNotificationRequest.Timestamp, System.Globalization.CultureInfo.InvariantCulture), chargePointStatus.Id) == false)
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
                            _logger.LogTrace("StatusNotification => New online connector status {ChargePointId} connector {ConnectorId}: {Status}", chargePointStatus?.Id, connectorId, newStatus);
                        }
                        else
                        {
                            _logger.LogError("StatusNotification => Could not add online connector status for {ChargePointId} connector {ConnectorId}", chargePointStatus?.Id, connectorId);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("StatusNotification => Status for unexpected connector {ConnectorId} on {ChargePointId}", connectorId, chargePointStatus?.Id);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(statusNotificationResponse, OCPPMessageFactory.DefaultSettings);
                _logger.LogTrace("StatusNotification => Response serialized");
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "StatusNotification => Exception processing request from {ChargePointId}", chargePointStatus.Id);
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