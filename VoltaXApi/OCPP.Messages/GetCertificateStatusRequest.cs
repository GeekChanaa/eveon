namespace VoltaXApi.OCPP.Messages
{
  using System.ComponentModel.DataAnnotations;

  public class GetCertificateStatusRequest
  {
      [Required]
      public OCSPRequestDataType OcspRequestData { get; set; }

      public CustomDataType CustomData { get; set; }
  }
  

}