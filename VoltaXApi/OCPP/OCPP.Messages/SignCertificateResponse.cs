using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class SignCertificateResponse
  {
      public CustomDataType? customData { get; set; }

      [Required]
      public GenericStatusEnumType status { get; set; }

      public StatusInfoType? statusInfo { get; set; }
  }

}