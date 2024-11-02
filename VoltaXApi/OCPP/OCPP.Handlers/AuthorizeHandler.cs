using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class AuthorizeHandler : IOCPPRequestHandler
  {
    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly IChargeTagRepository _chargeTagRepository;

    public AuthorizeHandler(
      ILoggerFactory loggerFactory,
      IChargeTagRepository chargeTagRepository,
      IMessageLogRepository messageLogRepository
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(AuthorizeHandler));
      _chargeTagRepository = chargeTagRepository;
      _msgLogRepo = messageLogRepository;
    }

      public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
      {
          string? errorCode = null;
          AuthorizeResponse authorizeResponse = new AuthorizeResponse();

          string? idTag = null;
          try
          {
              _logger.LogTrace("Processing authorize request...");
              AuthorizeRequest authorizeRequest = JsonConvert.DeserializeObject<AuthorizeRequest>(msgIn.JsonPayload);
              _logger.LogTrace("Authorize => Message deserialized");
              idTag = authorizeRequest?.IdToken?.IdToken;

              authorizeResponse.CustomData = new CustomDataType();
              authorizeResponse.CustomData.VendorId = OCPPHelper.VendorId;

              authorizeResponse.IdTokenInfo = new IdTokenInfoType();
              authorizeResponse.IdTokenInfo.CustomData = new CustomDataType();
              authorizeResponse.IdTokenInfo.CustomData.VendorId = OCPPHelper.VendorId;

              try
              {
                  var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
              
                  // ChargeTag ct = await _chargeTagRepository.GetByTagId(idTag);
                  // if (ct != null)
                  // {
                  //     if (!string.IsNullOrEmpty(ct.ParentTagId))
                  //     {
                  //         authorizeResponse.IdTokenInfo.GroupIdToken.IdToken = ct.ParentTagId;
                  //     }

                  //     if (ct.Blocked.HasValue && ct.Blocked.Value)
                  //     {
                  //         authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                  //     }
                  //     else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                  //     {
                  //         authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                  //     }
                  //     else
                  //     {
                  //         authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                  //     }
                  // }
                  // else
                  // {
                  //     authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                  // }

                  authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                  _logger.LogInformation("Authorize => Status: {0}", authorizeResponse.IdTokenInfo.Status);
              }
              catch (Exception exp)
              {
                  _logger.LogError(exp, "Authorize => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                  authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
              }
              
              authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
              
              var settings = new JsonSerializerSettings
              {
                  Converters = new List<JsonConverter> { new StringEnumConverter() }
              };
              msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse, settings);
              _logger.LogTrace("Authorize => Response serialized");
          }
          catch (Exception exp)
          {
              _logger.LogError(exp, "Authorize => Exception: {0}", exp.Message);
              errorCode = ErrorCodes.FormationViolation;
          }

          await _msgLogRepo.SaveLogMessage(chargePointStatus?.Id, null, msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTokenInfo?.Status}", errorCode);
          return errorCode;
      }
  }
}