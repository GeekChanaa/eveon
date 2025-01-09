using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
    public class TransactionEventHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ITransactionService _transactionService;
        private readonly IConnectorRepository _connectorRepository;
        private readonly IChargePointRepository _chargePointRepository;
        private readonly ILogger _logger;

        public TransactionEventHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            ITransactionService transactionService,
            IConnectorRepository connectorRepository,
            IChargePointRepository chargePointRepository
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(TransactionEventHandler));
            _msgLogRepo = messageLogRepository;
            _transactionService = transactionService;
            _connectorRepository = connectorRepository;
            _chargePointRepository = chargePointRepository;
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

                //  Extract meter values with correct scale
                double currentChargeKW = -1;
                double meterKWH = -1;
                DateTimeOffset? meterTime = null;
                double stateOfCharge = -1;
                if(transactionEventRequest.MeterValue != null)
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
                msgOut.JsonPayload = JsonConvert.SerializeObject(transactionEventResponse,settings);
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
            currentChargeKW = -1;
            meterKWH = -1;
            meterTime = null;
            stateOfCharge = -1;

            foreach (MeterValueType meterValue in meterValues)
            {
                foreach (SampledValueType sampleValue in meterValue.SampledValue)
                {
                    Console.WriteLine("this is the samledValueType : "+ sampleValue.Measurand );
                    Console.WriteLine("this is the context : "+ sampleValue.Context );
                    Console.WriteLine("context is transaction : "+ sampleValue.Context );
                    if(sampleValue.Context == ReadingContextEnumType.Transaction_End)
                    {
                        meterKWH = sampleValue.Value;
                         if (
                            sampleValue.UnitOfMeasure?.Unit == "W"
                            || sampleValue.UnitOfMeasure?.Unit == "VA"
                            || sampleValue.UnitOfMeasure?.Unit == "var"
                            || sampleValue.UnitOfMeasure?.Unit == null
                            || sampleValue.UnitOfMeasure == null
                        )
                        {
                            Console.WriteLine(
                                "GetMeterValues => Charging '{0:0.0}' W",
                                currentChargeKW
                            );
                            // convert W => kW
                            currentChargeKW = currentChargeKW / 1000;
                            meterKWH = meterKWH / 1000;
                        }
                        else if (
                            sampleValue.UnitOfMeasure?.Unit == "KW"
                            || sampleValue.UnitOfMeasure?.Unit == "kVA"
                            || sampleValue.UnitOfMeasure?.Unit == "kvar"
                        )
                        {
                            // already kW => OK
                            Console.WriteLine(
                                "GetMeterValues => Charging '{0:0.0}' kW",
                                currentChargeKW
                            );
                        }
                        return;
                    }
                    if (sampleValue.Measurand == MeasurandEnumType.Power_Active_Import)
                    {
                        // current charging power
                        currentChargeKW = sampleValue.Value;
                        if (
                            sampleValue.UnitOfMeasure?.Unit == "W"
                            || sampleValue.UnitOfMeasure?.Unit == "VA"
                            || sampleValue.UnitOfMeasure?.Unit == "var"
                            || sampleValue.UnitOfMeasure?.Unit == null
                            || sampleValue.UnitOfMeasure == null
                        )
                        {
                            Console.WriteLine(
                                "GetMeterValues => Charging '{0:0.0}' W",
                                currentChargeKW
                            );
                            // convert W => kW
                            currentChargeKW = currentChargeKW / 1000;
                        }
                        else if (
                            sampleValue.UnitOfMeasure?.Unit == "KW"
                            || sampleValue.UnitOfMeasure?.Unit == "kVA"
                            || sampleValue.UnitOfMeasure?.Unit == "kvar"
                        )
                        {
                            // already kW => OK
                            Console.WriteLine(
                                "GetMeterValues => Charging '{0:0.0}' kW",
                                currentChargeKW
                            );
                        }
                        else
                        {
                            Console.WriteLine(
                                "GetMeterValues => Charging: unexpected unit: '{0}' (Value={1})",
                                sampleValue.UnitOfMeasure?.Unit,
                                sampleValue.Value
                            );
                        }
                    }
                    else if (
                        sampleValue.Measurand == MeasurandEnumType.Energy_Active_Import_Register
                    )
                    {
                        Console.WriteLine("okay this is : Energy_Active_Import_Register");
                        // charged amount of energy
                        meterKWH = sampleValue.Value;
                        if (
                            sampleValue.UnitOfMeasure?.Unit == "Wh"
                            || sampleValue.UnitOfMeasure?.Unit == "VAh"
                            || sampleValue.UnitOfMeasure?.Unit == "varh"
                            || (
                                sampleValue.UnitOfMeasure == null
                                || sampleValue.UnitOfMeasure.Unit == null
                            )
                        )
                        {
                            // Multiplying this by the meter value
                            Console.WriteLine("GetMeterValues => Value: '{0:0.0}' Wh", meterKWH);
                            if (
                                sampleValue.UnitOfMeasure?.Multiplier != null
                                && sampleValue.UnitOfMeasure?.Multiplier > 0
                            )
                                meterKWH =
                                    meterKWH
                                    * Math.Pow(10, (double)sampleValue.UnitOfMeasure?.Multiplier);

                            // convert Wh => kWh
                            meterKWH = meterKWH / 1000;
                        }
                        else if (
                            sampleValue.UnitOfMeasure?.Unit == "kWh"
                            || sampleValue.UnitOfMeasure?.Unit == "kVAh"
                            || sampleValue.UnitOfMeasure?.Unit == "kvarh"
                        )
                        {
                            // already kWh => OK
                            Console.WriteLine("GetMeterValues => Value: '{0:0.0}' kWh", meterKWH);
                        }
                        else
                        {
                            Console.WriteLine(
                                "GetMeterValues => Value: unexpected unit: '{0}' (Value={1})",
                                sampleValue.UnitOfMeasure?.Unit,
                                sampleValue.Value
                            );
                        }
                        meterTime = meterValue.Timestamp;
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.SoC)
                    {
                        // state of charge (battery status)
                        stateOfCharge = sampleValue.Value;
                        Console.WriteLine("GetMeterValues => SoC: '{0:0.0}'%", stateOfCharge);
                    }
                }
            }
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
