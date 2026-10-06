using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class AuthorizeHandler : IOCPPRequestHandler
  {
    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly IdTokenAuthorizationService _authorization;

    public AuthorizeHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository,
      IdTokenAuthorizationService authorization
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(AuthorizeHandler));
      _msgLogRepo = messageLogRepository;
      _authorization = authorization;
    }

    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
      string? errorCode = null;
      var authorizeResponse = new AuthorizeResponse
      {
        CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId },
        IdTokenInfo = new IdTokenInfoType
        {
          CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId }
        }
      };

      string? idTag = null;
      try
      {
        var authorizeRequest = JsonConvert.DeserializeObject<AuthorizeRequest>(msgIn.JsonPayload ?? string.Empty);
        idTag = authorizeRequest?.IdToken?.IdToken;

        authorizeResponse.IdTokenInfo.Status = await _authorization.AuthorizeAsync(idTag, chargePointStatus.Id);

        _logger.LogInformation("Authorize => {ChargePointId} token status {Status}", chargePointStatus.Id, authorizeResponse.IdTokenInfo.Status);
        msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse, OCPPMessageFactory.DefaultSettings);
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "Authorize => Exception processing request from {ChargePointId}", chargePointStatus.Id);
        errorCode = ErrorCodes.FormationViolation;
      }

      await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTokenInfo?.Status}", errorCode!, msgIn, msgOut);
      return errorCode!;
    }
  }
}
