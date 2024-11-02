


using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{

  public class UnlockConnectorHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    public UnlockConnectorHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository
    )
    {
        _logger = loggerFactory.CreateLogger(typeof(UnlockConnectorHandler));
        _msgLogRepo = messageLogRepository;
    }


    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
      _logger.LogInformation("UnlockConnector answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", chargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);
      string? errorCode = null;
      try
      {
        UnlockConnectorResponse unlockConnectorResponse = JsonConvert.DeserializeObject<UnlockConnectorResponse>(msgIn.JsonPayload);
        _logger.LogInformation("HandleUnlockConnector => Answer status: {0}", unlockConnectorResponse?.Status);
        await _msgLogRepo.SaveLogMessage(chargePointStatus?.Id, null, msgOut.Action, unlockConnectorResponse?.Status.ToString(), msgIn.ErrorCode);

        if (msgOut.TaskCompletionSource != null)
        {
          // Set API response as TaskCompletion-result
          string apiResult = "{\"status\": " + JsonConvert.ToString(unlockConnectorResponse.Status.ToString()) + "}";
          _logger.LogTrace("HandleUnlockConnector => API response: {0}", apiResult);

          msgOut.TaskCompletionSource.SetResult(apiResult);
        }
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "HandleUnlockConnector => Exception: {0}", exp.Message);
      }
      return errorCode;
    }
  }
  
}