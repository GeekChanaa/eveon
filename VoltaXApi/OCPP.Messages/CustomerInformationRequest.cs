namespace VoltaXApi.OCPP.Messages
{
  public class CustomerInformationRequest
  {
      public CustomDataType CustomData { get; set; }
      public CertificateHashData CertificateHashData { get; set; }
      public IdTokenType IdToken { get; set; }
      public int RequestId { get; set; }
      public bool Report { get; set; }
      public bool Clear { get; set; }
      public string CustomerIdentifier { get; set; }
  }

  public class CertificateHashData
  {
      public CustomDataType CustomData { get; set; }
      public HashAlgorithmEnum HashAlgorithm { get; set; }
      public string IssuerNameHash { get; set; }
      public string IssuerKeyHash { get; set; }
      public string SerialNumber { get; set; }
  }

  public enum HashAlgorithmEnum
  {
      SHA256,
      SHA384,
      SHA512
  }

}