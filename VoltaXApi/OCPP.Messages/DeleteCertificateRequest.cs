namespace VoltaXApi.OCPP.Messages
{
  public class DeleteCertificateRequest
  {
      public CustomDataType CustomData { get; set; }
      public CertificateHashDataType CertificateHashData { get; set; }
  }

  public class CustomDataType
  {
      public string VendorId { get; set; }
  }

}