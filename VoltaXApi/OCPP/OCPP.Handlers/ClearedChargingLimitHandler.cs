using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Models;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>ClearedChargingLimit (2.0.1): the external limits of a source are lifted; their stored rows are marked Cleared.</summary>
    public class ClearedChargingLimitHandler : IOCPPRequestHandler
    {
        private readonly ILogger _logger;
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly SmartChargingInboundStore _store;

        public ClearedChargingLimitHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            SmartChargingInboundStore store)
        {
            _logger = loggerFactory.CreateLogger(typeof(ClearedChargingLimitHandler));
            _msgLogRepo = messageLogRepository;
            _store = store;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            string? source = null;
            int? evseId = null;

            var clearedChargingLimitResponse = new ClearedChargingLimitResponse
            {
                CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId }
            };

            try
            {
                var clearedChargingLimitRequest = JsonConvert.DeserializeObject<ClearedChargingLimitRequest>(msgIn.JsonPayload ?? string.Empty);
                if (clearedChargingLimitRequest == null)
                {
                    errorCode = ErrorCodes.FormationViolation;
                }
                else
                {
                    source = clearedChargingLimitRequest.ChargingLimitSource.ToString();
                    evseId = clearedChargingLimitRequest.EvseId;
                    _logger.LogInformation("ClearedChargingLimit => {ChargePointId} EVSE {EvseId} Source={Source}", chargePointStatus.Id, evseId, source);
                    await _store.ClearChargingLimitAsync(chargePointStatus.Id, source, evseId);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(clearedChargingLimitResponse, OCPPMessageFactory.DefaultSettings);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "ClearedChargingLimit => Exception processing request from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, evseId, msgIn.Action, source!, errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }
}
