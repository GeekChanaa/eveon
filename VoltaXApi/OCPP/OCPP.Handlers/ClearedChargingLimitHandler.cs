using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class ClearedChargingLimitHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;

    public ClearedChargingLimitHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(ClearedChargingLimitHandler));
      _msgLogRepo = messageLogRepository;
    }

      public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
      {

          string errorCode = null;

          _logger.LogTrace("Processing ClearedChargingLimit...");
          ClearedChargingLimitResponse clearedChargingLimitResponse = new ClearedChargingLimitResponse();
          clearedChargingLimitResponse.CustomData = new CustomDataType();
          clearedChargingLimitResponse.CustomData.VendorId = OCPPHelper.VendorId;

          try
          {
              ClearedChargingLimitRequest clearedChargingLimitRequest = JsonConvert.DeserializeObject<ClearedChargingLimitRequest>(msgIn.JsonPayload);
              _logger.LogTrace("ClearedChargingLimit => Message deserialized");

              // if (ChargePointStatus != null)
              // {
              //     // Known charge station
              //     source = clearedChargingLimitRequest.ChargingLimitSource.ToString();
              //     connectorId = clearedChargingLimitRequest.EvseId;
              //     _logger.LogInformation("ClearedChargingLimit => Source={0}", source);
              // }
              // else
              // {
              //     // Unknown charge station
              //     errorCode = ErrorCodes.GenericError;
              // }

              msgOut.JsonPayload = JsonConvert.SerializeObject(clearedChargingLimitResponse);
              _logger.LogTrace("ClearedChargingLimit => Response serialized");
          }
          catch (Exception exp)
          {
              _logger.LogError(exp, "ClearedChargingLimit => Exception: {0}", exp.Message);
              errorCode = ErrorCodes.InternalError;
          }

          // await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, source, errorCode);
          return errorCode;
      }
  }
}