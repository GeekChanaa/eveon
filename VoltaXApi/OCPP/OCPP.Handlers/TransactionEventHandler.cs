using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    public class TransactionEventHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ITransactionService _transactionService;
        private readonly IConnectorRepository _connectorRepository;
        private readonly IChargePointRepository _chargePointRepository;
        private readonly IConfigurationService _configService;
        private readonly ILogger _logger;

        private static readonly JsonSerializerSettings ResponseSerializerSettings = new()
        {
            Converters = new List<JsonConverter> { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        public TransactionEventHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            ITransactionService transactionService,
            IConnectorRepository connectorRepository,
            IChargePointRepository chargePointRepository,
            IConfigurationService configService
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(TransactionEventHandler));
            _msgLogRepo = messageLogRepository;
            _transactionService = transactionService;
            _connectorRepository = connectorRepository;
            _chargePointRepository = chargePointRepository;
            _configService = configService;
        }

        public async Task<string> Handle(
            OCPPMessage msgIn,
            OCPPMessage msgOut,
            ChargePointStatus chargePointStatus
        )
        {
            string errorCode = null!;
            var transactionEventResponse = new TransactionEventResponse
            {
                CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId },
                IdTokenInfo = new IdTokenInfoType()
            };

            int connectorId = 0;

            try
            {
                _logger.LogInformation("TransactionEvent => Processing request from {ChargePointId}", chargePointStatus.Id);

                var transactionEventRequest = JsonConvert.DeserializeObject<TransactionEventRequest>(msgIn.JsonPayload ?? string.Empty);
                if (transactionEventRequest == null)
                {
                    _logger.LogWarning("TransactionEvent => Failed to deserialize request payload");
                    errorCode = ErrorCodes.FormationViolation;
                    return errorCode;
                }

                string idTag = transactionEventRequest.IdToken != null
                    ? CleanChargeTagId(transactionEventRequest.IdToken.IdToken)
                    : string.Empty;

                var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);
                if (chargePoint == null)
                {
                    _logger.LogWarning("TransactionEvent => Charge point not found: {ChargePointId}", chargePointStatus.Id);
                    errorCode = ErrorCodes.GenericError;
                    return errorCode;
                }

                var connector = await _connectorRepository.GetConnectorByConnectorIdEvseId(
                    (int)transactionEventRequest.EVSE.ConnectorId,
                    (int)transactionEventRequest.EVSE.Id,
                    chargePoint.ID);

                if (connector == null)
                {
                    _logger.LogWarning("TransactionEvent => Connector not found, refreshing for {ChargePointId}", chargePointStatus.Id);
                    await _configService.RefreshConnectors(chargePointStatus.Id);

                    connector = await _connectorRepository.GetConnectorByConnectorIdEvseId(
                        (int)transactionEventRequest.EVSE.ConnectorId,
                        (int)transactionEventRequest.EVSE.Id,
                        chargePoint.ID);

                    if (connector == null)
                    {
                        _logger.LogError("TransactionEvent => Connector still not found after refresh for {ChargePointId}", chargePointStatus.Id);
                        errorCode = ErrorCodes.GenericError;
                        return errorCode;
                    }
                }

                connectorId = connector.ID;

                var meterData = transactionEventRequest.MeterValue != null
                    ? ExtractMeterValues(transactionEventRequest.MeterValue)
                    : MeterData.Empty;

                switch (transactionEventRequest.EventType)
                {
                    case TransactionEventEnumType.Started:
                        await _transactionService.StartTransaction(
                            transactionEventRequest, transactionEventResponse,
                            chargePointStatus, connector, idTag, errorCode, meterData.EnergyKWh);
                        break;

                    case TransactionEventEnumType.Updated:
                        await _transactionService.UpdateTransaction(
                            transactionEventRequest, transactionEventResponse,
                            chargePointStatus, connector, idTag, errorCode, meterData.EnergyKWh);
                        break;

                    case TransactionEventEnumType.Ended:
                        await _transactionService.EndTransaction(
                            transactionEventRequest, transactionEventResponse,
                            chargePointStatus, connector, idTag, errorCode, meterData.EnergyKWh);
                        break;

                    default:
                        _logger.LogWarning("TransactionEvent => Unknown event type: {EventType}", transactionEventRequest.EventType);
                        break;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(transactionEventResponse, ResponseSerializerSettings);
                _logger.LogInformation("TransactionEvent => Response serialized for {EventType}", transactionEventRequest.EventType);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "TransactionEvent => Exception processing request from {ChargePointId}", chargePointStatus?.Id);
                errorCode = ErrorCodes.FormationViolation;
            }

            await _msgLogRepo.SaveLogMessage(
                chargePointStatus?.Id ?? string.Empty,
                connectorId,
                msgIn.Action,
                transactionEventResponse.IdTokenInfo?.Status.ToString() ?? string.Empty,
                errorCode ?? string.Empty,
                msgIn,
                msgOut
            );

            return errorCode;
        }

        #region Meter Value Extraction

        /// <summary>
        /// Structured result from meter value extraction.
        /// </summary>
        private record MeterData(
            double EnergyKWh,
            double PowerKW,
            double StateOfCharge,
            DateTimeOffset? Timestamp)
        {
            public static readonly MeterData Empty = new(0, 0, 0, null);
        }

        /// <summary>
        /// Extract all meter values from the OCPP MeterValue collection.
        /// </summary>
        private MeterData ExtractMeterValues(ICollection<MeterValueType> meterValues)
        {
            double meterKWH = 0;
            double currentChargeKW = 0;
            double stateOfCharge = 0;
            DateTimeOffset? meterTime = null;

            foreach (var meterValue in meterValues)
            {
                foreach (var sample in meterValue.SampledValue)
                {
                    var unit = sample.UnitOfMeasure?.Unit;
                    var multiplier = sample.UnitOfMeasure?.Multiplier ?? 0;
                    var value = sample.Value;

                    // Context-based: Transaction_End is the final reading
                    if (sample.Context == ReadingContextEnumType.Transaction_End)
                    {
                        meterKWH = ConvertToKWh(value, unit, multiplier);
                        meterTime = meterValue.Timestamp;
                        _logger.LogDebug("MeterValues => Transaction_End: {Energy:0.000} kWh", meterKWH);
                        return new MeterData(meterKWH, currentChargeKW, stateOfCharge, meterTime);
                    }

                    if (sample.Context == ReadingContextEnumType.Transaction_Begin)
                    {
                        meterKWH = ConvertToKWh(value, unit, multiplier);
                        meterTime = meterValue.Timestamp;
                        _logger.LogDebug("MeterValues => Transaction_Begin: {Energy:0.000} kWh", meterKWH);
                    }

                    // Measurand-based handling
                    switch (sample.Measurand)
                    {
                        case MeasurandEnumType.Power_Active_Import:
                            currentChargeKW = ConvertToKW(value, unit, multiplier);
                            _logger.LogDebug("MeterValues => Power: {Power:0.00} kW", currentChargeKW);
                            break;

                        case MeasurandEnumType.Energy_Active_Import_Register:
                            meterKWH = ConvertToKWh(value, unit, multiplier);
                            meterTime = meterValue.Timestamp;
                            _logger.LogDebug("MeterValues => Energy: {Energy:0.000} kWh", meterKWH);
                            break;

                        case MeasurandEnumType.SoC:
                            stateOfCharge = value;
                            _logger.LogDebug("MeterValues => SoC: {SoC:0.0}%", stateOfCharge);
                            break;
                    }
                }
            }

            return new MeterData(meterKWH, currentChargeKW, stateOfCharge, meterTime);
        }

        #endregion

        #region Unit Conversion

        /// <summary>
        /// Convert energy values (Wh, kWh, etc.) to kWh.
        /// </summary>
        private double ConvertToKWh(double value, string? unit, int multiplier)
        {
            if (multiplier > 0)
                value *= Math.Pow(10, multiplier);

            if (string.IsNullOrEmpty(unit) || unit == "Wh" || unit == "VAh" || unit == "varh")
                return value / 1000.0;

            if (unit == "kWh" || unit == "kVAh" || unit == "kvarh")
                return value;

            _logger.LogWarning("MeterValues => Unexpected energy unit: {Unit}, Value={Value}", unit, value);
            return value;
        }

        /// <summary>
        /// Convert power values (W, kW, etc.) to kW.
        /// </summary>
        private double ConvertToKW(double value, string? unit, int multiplier)
        {
            if (multiplier > 0)
                value *= Math.Pow(10, multiplier);

            if (string.IsNullOrEmpty(unit) || unit == "W" || unit == "VA" || unit == "var")
                return value / 1000.0;

            if (unit == "kW" || unit == "kVA" || unit == "kvar")
                return value;

            _logger.LogWarning("MeterValues => Unexpected power unit: {Unit}, Value={Value}", unit, value);
            return value;
        }

        #endregion

        /// <summary>
        /// Clean vendor-specific suffixes from charge tag IDs (e.g., KEBA appends "_serial").
        /// </summary>
        private string CleanChargeTagId(string rawChargeTagId)
        {
            if (string.IsNullOrWhiteSpace(rawChargeTagId))
                return string.Empty;

            int sep = rawChargeTagId.IndexOf('_');
            if (sep >= 0)
            {
                var cleaned = rawChargeTagId[..sep];
                _logger.LogDebug("CleanChargeTagId => '{Raw}' => '{Cleaned}'", rawChargeTagId, cleaned);
                return cleaned;
            }

            return rawChargeTagId;
        }
    }
}
