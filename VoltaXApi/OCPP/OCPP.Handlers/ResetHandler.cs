

using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    public class ResetHandler : IOCPPRequestHandler
    {

        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ILogger _logger;
        public ResetHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository
        )
        {
            _msgLogRepo = messageLogRepository;
            _logger = loggerFactory.CreateLogger(typeof(ResetHandler));
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            _logger.LogInformation("Reset answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", chargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);
            string? errorCode = null;

            try
            {
                ResetResponse resetResponse = JsonConvert.DeserializeObject<ResetResponse>(msgIn.JsonPayload);
                _logger.LogInformation("Reset => Answer status: {0}", resetResponse?.Status);
                await _msgLogRepo.SaveLogMessage(chargePointStatus?.Id, null, msgOut.Action, resetResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(resetResponse.Status.ToString()) + "}";
                    _logger.LogTrace("HandleReset => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "HandleReset => Exception: {0}", exp.Message);
            }
            return errorCode;
        }
    }
}