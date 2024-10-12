using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  
  public class SendLocalListRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MinLength(1)]
      public List<AuthorizationData> LocalAuthorizationList { get; set; }

      [Required]
      public int VersionNumber { get; set; }

      [Required]
      public UpdateEnum UpdateType { get; set; }
  }

  public class AuthorizationData
  {
      [Required]
      public IdTokenType IdToken { get; set; }

      public CustomDataType CustomData { get; set; }

      public IdTokenInfoType IdTokenInfo { get; set; }
  }

  public enum UpdateEnum
  {
      Differential,
      Full
  }

  public enum IdTokenEnum
  {
      Central,
      eMAID,
      ISO14443,
      ISO15693,
      KeyCode,
      Local,
      MacAddress,
      NoAuthorization
  }

  public enum AuthorizationStatusEnum
  {
      Accepted,
      Blocked,
      ConcurrentTx,
      Expired,
      Invalid,
      NoCredit,
      NotAllowedTypeEVSE,
      NotAtThisLocation,
      NotAtThisTime,
      Unknown
  }

}