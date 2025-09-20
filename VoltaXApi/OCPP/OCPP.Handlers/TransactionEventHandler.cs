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
            string? errorCode = null;
            TransactionEventResponse transactionEventResponse = new TransactionEventResponse();
            transactionEventResponse.CustomData = new CustomDataType();
            transactionEventResponse.CustomData.VendorId = OCPPHelper.VendorId;
            transactionEventResponse.IdTokenInfo = new IdTokenInfoType();

            int connectorId = 0;

            try
            {
                Console.WriteLine("TransactionEvent => Processing transactionEvent request...");

                TransactionEventRequest transactionEventRequest =
                    JsonConvert.DeserializeObject<TransactionEventRequest>(msgIn.JsonPayload);

                string idTag = "";
                if (transactionEventRequest.IdToken != null)
                    idTag = CleanChargeTagId(transactionEventRequest.IdToken.IdToken, _logger);

                var chargePoint = await this._chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);

                var connector = await this._connectorRepository
                    .GetConnectorByConnectorIdEvseId(
                        (int)transactionEventRequest.EVSE.ConnectorId,
                        (int)transactionEventRequest.EVSE.Id,
                        chargePoint.ID);

                if (connector == null)
                {
                    await _configService.RefreshConnectors(chargePointStatus.Id);
                }


                //  Extract meter values with correct scale
                double currentChargeKW = 0;
                double meterKWH = 0;
                DateTimeOffset? meterTime = null;
                double stateOfCharge = -1;
                if (transactionEventRequest.MeterValue != null)
                    GetMeterValues(
                        transactionEventRequest.MeterValue,
                        out meterKWH,
                        out currentChargeKW,
                        out stateOfCharge,
                        out meterTime
                    );

                if (transactionEventRequest.EventType == TransactionEventEnumType.Started)
                {
                    await _transactionService.StartTransaction(
                        transactionEventRequest,
                        transactionEventResponse,
                        chargePointStatus,
                        connector,
                        idTag,
                        errorCode,
                        meterKWH
                    );
                }
                else if (transactionEventRequest.EventType == TransactionEventEnumType.Updated)
                {
                    await _transactionService.UpdateTransaction(
                        transactionEventRequest,
                        transactionEventResponse,
                        chargePointStatus,
                        connector,
                        idTag,
                        errorCode,
                        meterKWH
                    );
                }
                else if (transactionEventRequest.EventType == TransactionEventEnumType.Ended)
                {
                    await _transactionService.EndTransaction(
                        transactionEventRequest,
                        transactionEventResponse,
                        chargePointStatus,
                        connector,
                        idTag,
                        errorCode,
                        meterKWH
                    );
                }

                var settings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { new StringEnumConverter() },
                    NullValueHandling = NullValueHandling.Ignore
                };
                msgOut.JsonPayload = JsonConvert.SerializeObject(transactionEventResponse, settings);
                Console.WriteLine("TransactionEvent => Response serialized");
            }
            catch (Exception exp)
            {
                Console.WriteLine("TransactionEvent => Exception: {0}", exp.Message);
                Console.WriteLine(exp.StackTrace);
                errorCode = ErrorCodes.FormationViolation;
            }

            await _msgLogRepo.SaveLogMessage(
                chargePointStatus?.Id,
                connectorId,
                msgIn.Action,
                transactionEventResponse.IdTokenInfo.Status.ToString(),
                errorCode,
                msgIn,
                msgOut
            );
            return errorCode;
        }

        /// <summary>
        /// Extract main meter value from collection
        /// </summary>
        private double GetMeterValue(ICollection<MeterValueType> meterValues)
        {
            double currentChargeKW = -1;
            double meterKWH = -1;
            DateTimeOffset? meterTime = null;
            double stateOfCharge = -1;
            GetMeterValues(
                meterValues,
                out meterKWH,
                out currentChargeKW,
                out stateOfCharge,
                out meterTime
            );

            return meterKWH;
        }

        /// <summary>
        /// Extract different meter values from collection
        /// </summary>
        private void GetMeterValues(
            ICollection<MeterValueType> meterValues,
            out double meterKWH,
            out double currentChargeKW,
            out double stateOfCharge,
            out DateTimeOffset? meterTime
        )
        {
            meterKWH = 0;
            currentChargeKW = 0;
            stateOfCharge = 0;
            meterTime = null;

            foreach (var meterValue in meterValues)
            {
                foreach (var sampleValue in meterValue.SampledValue)
                {
                    var unit = sampleValue.UnitOfMeasure?.Unit;
                    var multiplier = sampleValue.UnitOfMeasure?.Multiplier ?? 0;
                    var value = sampleValue.Value;

                    switch (sampleValue.Context)
                    {
                        case ReadingContextEnumType.Transaction_End:
                            meterKWH = ConvertToKWh(value, unit, multiplier);
                            meterTime = meterValue.Timestamp;
                            Console.WriteLine($"GetMeterValues => Transaction_End: {meterKWH:0.000} kWh");
                            return; // final reading, safe to exit

                        case ReadingContextEnumType.Transaction_Begin:
                            Console.WriteLine("GetMeterValues => Transaction_Begin context detected.");
                            meterKWH = ConvertToKWh(value, unit, multiplier);
                            meterTime = meterValue.Timestamp;
                            break;

                        case ReadingContextEnumType.Interruption_Begin:
                            Console.WriteLine("GetMeterValues => Interruption_Begin context detected.");
                            break;

                        case ReadingContextEnumType.Interruption_End:
                            Console.WriteLine("GetMeterValues => Interruption_End context detected.");
                            break;

                        case ReadingContextEnumType.Sample_Clock:
                            Console.WriteLine("GetMeterValues => Sample_Clock reading.");
                            break;

                        case ReadingContextEnumType.Sample_Periodic:
                            Console.WriteLine("GetMeterValues => Sample_Periodic reading.");
                            break;

                        case ReadingContextEnumType.Trigger:
                            Console.WriteLine("GetMeterValues => Triggered reading.");
                            break;

                        case ReadingContextEnumType.Other:
                        default:
                            // fall back to measurand-based handling
                            break;
                    }

                    if (sampleValue.Measurand == MeasurandEnumType.Power_Active_Import)
                    {
                        currentChargeKW = ConvertToKW(value, unit, multiplier);
                        Console.WriteLine($"GetMeterValues => Charging: {currentChargeKW:0.00} kW");
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.Energy_Active_Import_Register)
                    {
                        meterKWH = ConvertToKWh(value, unit, multiplier);
                        meterTime = meterValue.Timestamp;
                        Console.WriteLine($"GetMeterValues => Energy Imported: {meterKWH:0.000} kWh");
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.SoC)
                    {
                        stateOfCharge = value;
                        Console.WriteLine($"GetMeterValues => SoC: {stateOfCharge:0.0}%");
                    }
                }
            }
        }


        /// <summary>
        /// Convert Wh or kWh values into kWh
        /// </summary>
        private double ConvertToKWh(double value, string unit, int multiplier)
        {
            if (string.IsNullOrEmpty(unit) || unit == "Wh" || unit == "VAh" || unit == "varh")
            {
                if (multiplier > 0)
                    value *= Math.Pow(10, multiplier);

                return value / 1000.0; // Wh → kWh
            }
            if (unit == "kWh" || unit == "kVAh" || unit == "kvarh")
            {
                return value;
            }

            Console.WriteLine($"GetMeterValues => Unexpected energy unit: {unit}, Value={value}");
            return value;
        }

        /// <summary>
        /// Convert W or kW values into kW
        /// </summary>
        private double ConvertToKW(double value, string unit, int multiplier)
        {
            if (string.IsNullOrEmpty(unit) || unit == "W" || unit == "VA" || unit == "var")
            {
                if (multiplier > 0)
                    value *= Math.Pow(10, multiplier);

                return value / 1000.0; // W → kW
            }
            if (unit == "kW" || unit == "kVA" || unit == "kvar")
            {
                return value; // already in kW
            }

            Console.WriteLine($"GetMeterValues => Unexpected power unit: {unit}, Value={value}");
            return value;
        }


        protected static string CleanChargeTagId(string rawChargeTagId, ILogger logger)
        {
            string idTag = rawChargeTagId;

            // KEBA adds the serial to the idTag ("<idTag>_<serial>") => cut off suffix
            if (!string.IsNullOrWhiteSpace(rawChargeTagId))
            {
                int sep = rawChargeTagId.IndexOf('_');
                if (sep >= 0)
                {
                    idTag = rawChargeTagId.Substring(0, sep);
                    Console.WriteLine(
                        "CleanChargeTagId => Charge tag '{0}' => '{1}'",
                        rawChargeTagId,
                        idTag
                    );
                }
            }

            return idTag;
        }
    }
}
