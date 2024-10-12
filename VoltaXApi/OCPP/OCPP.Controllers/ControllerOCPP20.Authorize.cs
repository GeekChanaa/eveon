using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP20
    {
        public string HandleAuthorize(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string? errorCode = null;
            AuthorizeResponse authorizeResponse = new AuthorizeResponse();

            string? idTag = null;
            try
            {
                Logger.LogTrace("Processing authorize request...");
                AuthorizeRequest authorizeRequest = JsonConvert.DeserializeObject<AuthorizeRequest>(msgIn.JsonPayload);
                Logger.LogTrace("Authorize => Message deserialized");
                idTag = CleanChargeTagId(authorizeRequest.IdToken?.IdToken, Logger);

                authorizeResponse.CustomData = new CustomDataType();
                authorizeResponse.CustomData.VendorId = VendorId;

                authorizeResponse.IdTokenInfo = new IdTokenInfoType();
                authorizeResponse.IdTokenInfo.CustomData = new CustomDataType();
                authorizeResponse.IdTokenInfo.CustomData.VendorId = VendorId;
                authorizeResponse.IdTokenInfo.GroupIdToken = new IdTokenType();
                authorizeResponse.IdTokenInfo.GroupIdToken.CustomData = new CustomDataType();
                authorizeResponse.IdTokenInfo.GroupIdToken.CustomData.VendorId = VendorId;
                authorizeResponse.IdTokenInfo.GroupIdToken.IdToken = string.Empty;

                try
                {
                    var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                    optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                    using (VoltaXApiDbContext dbContext = new VoltaXApiDbContext(optionsBuilder.Options))
                    {
                        // ChargeTag ct = dbContext.Find<ChargeTag>(idTag);
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
                        Logger.LogInformation("Authorize => Status: {0}", authorizeResponse.IdTokenInfo.Status);
                    }
                }
                catch (Exception exp)
                {
                    Logger.LogError(exp, "Authorize => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                    authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                }
                
                authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;

                msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                Logger.LogTrace("Authorize => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Authorize => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTokenInfo?.Status}", errorCode);
            return errorCode;
        }
    }
}
