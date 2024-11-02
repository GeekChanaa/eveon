using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
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
          string errorCode = null;
          MeterValuesResponse meterValuesResponse = new MeterValuesResponse();

          meterValuesResponse.CustomData = new CustomDataType();
          meterValuesResponse.CustomData.VendorId = OCPPHelper.VendorId;

          int connectorId = -1;
          string msgMeterValue = string.Empty;

          try
          {
              _logger.LogTrace("Processing meter values...");
              MeterValuesRequest meterValueRequest = JsonConvert.DeserializeObject<MeterValuesRequest>(msgIn.JsonPayload);
              _logger.LogTrace("MeterValues => Message deserialized");

              connectorId = meterValueRequest.EvseId;

              if (chargePointStatus != null)
              {
                  // Known charge station => extract meter values with correct scale
                  double currentChargeKW = -1;
                  double meterKWH = -1;
                  DateTimeOffset? meterTime = null;
                  double stateOfCharge = -1;
                  GetMeterValues(meterValueRequest.MeterValue, out meterKWH, out currentChargeKW, out stateOfCharge, out meterTime);

                  // write charging/meter data in chargepoint status
                  if (connectorId > 0)
                  {
                      msgMeterValue = $"Meter (kWh): {meterKWH} | Charge (kW): {currentChargeKW} | SoC (%): {stateOfCharge}";

                      if (currentChargeKW >= 0 || meterKWH >= 0 || stateOfCharge >= 0)
                      {
                          if (chargePointStatus.OnlineConnectors.ContainsKey(connectorId))
                          {
                              OnlineConnectorStatus ocs = chargePointStatus.OnlineConnectors[connectorId];
                              if (currentChargeKW >= 0) ocs.ChargeRateKW = currentChargeKW;
                              if (meterKWH >= 0) ocs.MeterKWH = meterKWH;
                              if (stateOfCharge >= 0) ocs.SoC = stateOfCharge;
                          }
                          else
                          {
                              OnlineConnectorStatus ocs = new OnlineConnectorStatus();
                              if (currentChargeKW >= 0) ocs.ChargeRateKW = currentChargeKW;
                              if (meterKWH >= 0) ocs.MeterKWH = meterKWH;
                              if (stateOfCharge >= 0) ocs.SoC = stateOfCharge;
                              if (chargePointStatus.OnlineConnectors.TryAdd(connectorId, ocs))
                              {
                                  _logger.LogTrace("MeterValues => Set OnlineConnectorStatus for ChargePoint={0} / Connector={1} / Values: {2}", chargePointStatus?.Id, connectorId, msgMeterValue);
                              }
                              else
                              {
                                  _logger.LogError("MeterValues => Error adding new OnlineConnectorStatus for ChargePoint={0} / Connector={1} / Values: {2}", chargePointStatus?.Id, connectorId, msgMeterValue);
                              }
                          }
                      }
                  }
              }
              else
              {
                  // Unknown charge station
                  errorCode = ErrorCodes.GenericError;
              }

              msgOut.JsonPayload = JsonConvert.SerializeObject(meterValuesResponse);
              _logger.LogTrace("MeterValues => Response serialized");
          }
          catch (Exception exp)
          {
              _logger.LogError(exp, "MeterValues => Exception: {0}", exp.Message);
              errorCode = ErrorCodes.InternalError;
          }

          await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, msgMeterValue, errorCode);
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
            GetMeterValues(meterValues, out meterKWH, out currentChargeKW, out stateOfCharge, out meterTime);

            return meterKWH;
        }

        /// <summary>
        /// Extract different meter values from collection
        /// </summary>
        private void GetMeterValues(ICollection<MeterValueType> meterValues, out double meterKWH, out double currentChargeKW, out double stateOfCharge, out DateTimeOffset? meterTime)
        {
            currentChargeKW = -1;
            meterKWH = -1;
            meterTime = null;
            stateOfCharge = -1;

            foreach (MeterValueType meterValue in meterValues)
            {
                
                foreach (SampledValueType sampleValue in meterValue.SampledValue)
                {
                    Console.WriteLine("this is the first metervaluetype : ");
                    Console.WriteLine(sampleValue.Value);
                    Console.WriteLine(sampleValue.Measurand);
                    Console.WriteLine("GetMeterValues => Context={0} / SignedMeterValue={1} / Value={2} / Unit={3} / Location={4} / Measurand={5} / Phase={6}",
                        sampleValue.Context, sampleValue.SignedMeterValue, sampleValue.Value, sampleValue.UnitOfMeasure, sampleValue.Location, sampleValue.Measurand, sampleValue.Phase);

                    if (sampleValue.Measurand == MeasurandEnumType.Power_Active_Import)
                    {
                        // current charging power
                        currentChargeKW = sampleValue.Value;
                        if (sampleValue.UnitOfMeasure?.Unit == "W" ||
                            sampleValue.UnitOfMeasure?.Unit == "VA" ||
                            sampleValue.UnitOfMeasure?.Unit == "var" ||
                            sampleValue.UnitOfMeasure?.Unit == null ||
                            sampleValue.UnitOfMeasure == null)
                        {
                            Console.WriteLine("GetMeterValues => Charging '{0:0.0}' W", currentChargeKW);
                            // convert W => kW
                            currentChargeKW = currentChargeKW / 1000;
                        }
                        else if (sampleValue.UnitOfMeasure?.Unit == "KW" ||
                                sampleValue.UnitOfMeasure?.Unit == "kVA" ||
                                sampleValue.UnitOfMeasure?.Unit == "kvar")
                        {
                            // already kW => OK
                            Console.WriteLine("GetMeterValues => Charging '{0:0.0}' kW", currentChargeKW);
                        }
                        else
                        {
                            Console.WriteLine("GetMeterValues => Charging: unexpected unit: '{0}' (Value={1})", sampleValue.UnitOfMeasure?.Unit, sampleValue.Value);
                        }
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.Energy_Active_Import_Register ||
                             sampleValue.Measurand == MeasurandEnumType.Missing)  
                    {
                        // charged amount of energy
                        meterKWH = sampleValue.Value;
                        if (sampleValue.UnitOfMeasure?.Unit == "Wh" ||
                            sampleValue.UnitOfMeasure?.Unit == "VAh" ||
                            sampleValue.UnitOfMeasure?.Unit == "varh" ||
                            (sampleValue.UnitOfMeasure == null || sampleValue.UnitOfMeasure.Unit == null))
                        {
                            // Multiplying this by the meter value
                            Console.WriteLine("GetMeterValues => Value: '{0:0.0}' Wh", meterKWH);
                            if(sampleValue.UnitOfMeasure?.Multiplier != null && sampleValue.UnitOfMeasure?.Multiplier>0)
                            meterKWH = meterKWH * Math.Pow(10,(double) sampleValue.UnitOfMeasure?.Multiplier);

                            // convert Wh => kWh
                            meterKWH = meterKWH / 1000;
                        }
                        else if (sampleValue.UnitOfMeasure?.Unit == "kWh" ||
                                sampleValue.UnitOfMeasure?.Unit == "kVAh" ||
                                sampleValue.UnitOfMeasure?.Unit == "kvarh")
                        {
                            // already kWh => OK
                            Console.WriteLine("GetMeterValues => Value: '{0:0.0}' kWh", meterKWH);
                        }
                        else
                        {
                            Console.WriteLine("GetMeterValues => Value: unexpected unit: '{0}' (Value={1})", sampleValue.UnitOfMeasure?.Unit, sampleValue.Value);
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
  }
}