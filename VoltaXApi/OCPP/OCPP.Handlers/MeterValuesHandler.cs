using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class MeterValuesHandler : IOCPPRequestHandler
  {
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly ILogger _logger;

    public MeterValuesHandler(
        ILoggerFactory loggerFactory,
        IMessageLogRepository messageLogRepository
    )
    {
        _logger = loggerFactory.CreateLogger(typeof(MeterValuesHandler));
        _msgLogRepo = messageLogRepository;
    }

    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
        string? errorCode = null;
        var meterValuesResponse = new MeterValuesResponse
        {
            CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId }
        };

        int connectorId = -1;
        string msgMeterValue = string.Empty;

        try
        {
            var meterValueRequest = JsonConvert.DeserializeObject<MeterValuesRequest>(msgIn.JsonPayload ?? string.Empty);
            if (meterValueRequest == null)
            {
                _logger.LogWarning("MeterValues => Invalid payload from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.FormationViolation;
            }
            else
            {
                connectorId = meterValueRequest.EvseId;
                var meter = MeterValueNormalizer.Extract(meterValueRequest.MeterValue);
                if (meter.UnexpectedUnits.Count > 0)
                    _logger.LogWarning("MeterValues => Unexpected meter units {Units} from {ChargePointId}; values used unconverted",
                        meter.UnexpectedUnits, chargePointStatus.Id);

                if (connectorId > 0 && (meter.EnergyKWh.HasValue || meter.PowerKW.HasValue || meter.StateOfCharge.HasValue))
                {
                    msgMeterValue = $"Meter (kWh): {meter.EnergyKWh} | Charge (kW): {meter.PowerKW} | SoC (%): {meter.StateOfCharge}";

                    if (!chargePointStatus.OnlineConnectors.TryGetValue(connectorId, out var ocs))
                    {
                        ocs = new OnlineConnectorStatus();
                        chargePointStatus.OnlineConnectors[connectorId] = ocs;
                    }
                    if (meter.PowerKW.HasValue) ocs.ChargeRateKW = meter.PowerKW;
                    if (meter.EnergyKWh.HasValue) ocs.MeterKWH = meter.EnergyKWh;
                    if (meter.StateOfCharge.HasValue) ocs.SoC = meter.StateOfCharge;

                    _logger.LogDebug("MeterValues => {ChargePointId} EVSE {EvseId}: {Values}", chargePointStatus.Id, connectorId, msgMeterValue);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(meterValuesResponse, OCPPMessageFactory.DefaultSettings);
            }
        }
        catch (Exception exp)
        {
            _logger.LogError(exp, "MeterValues => Exception processing request from {ChargePointId}", chargePointStatus.Id);
            errorCode = ErrorCodes.InternalError;
        }

        await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, msgMeterValue, errorCode!, msgIn, msgOut);
        return errorCode!;
    }
  }
}
