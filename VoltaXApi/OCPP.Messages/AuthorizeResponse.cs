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

}