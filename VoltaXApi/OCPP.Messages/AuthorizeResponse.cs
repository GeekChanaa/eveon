namespace VoltaXApi.OCPP.Messages
{
  public class AuthorizeResponse
  {
      public CustomDataType CustomData { get; set; }
      public IdTokenInfoType IdTokenInfo { get; set; }
      public AuthorizeCertificateStatusEnumType? CertificateStatus { get; set; }
  }

  public enum AuthorizationStatusEnumType
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

  public enum AuthorizeCertificateStatusEnumType
  {
      Accepted,
      SignatureError,
      CertificateExpired,
      CertificateRevoked,
      NoCertificateAvailable,
      CertChainError,
      ContractCancelled
  }

  public class IdTokenInfoType
  {
      public CustomDataType CustomData { get; set; }
      public AuthorizationStatusEnumType Status { get; set; }
      public DateTime? CacheExpiryDateTime { get; set; }
      public int? ChargingPriority { get; set; }
      public string Language1 { get; set; }
      public List<int> EvseId { get; set; }
      public IdTokenType GroupIdToken { get; set; }
      public string Language2 { get; set; }
      public MessageContentType PersonalMessage { get; set; }
  }
}