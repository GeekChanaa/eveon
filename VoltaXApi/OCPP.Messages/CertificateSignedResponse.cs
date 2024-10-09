namespace VoltaXApi.OCPP.Messages
{
  public class CertificateSignedResponse
  {
      public CustomDataType CustomData { get; set; }
      public CertificateSignedStatusEnumType Status { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum CertificateSignedStatusEnumType
  {
      Accepted,
      Rejected
  }

}