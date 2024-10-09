namespace VoltaXApi.OCPP.Messages
{
  public class DeleteCertificateRequest
  {
      public CustomDataType CustomData { get; set; }
      public CertificateHashDataType CertificateHashData { get; set; }
  }

}