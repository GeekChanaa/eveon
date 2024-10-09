  using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.OCPP.Messages
{

  public class Get15118EVCertificateResponse
  {
      [Required]
      public Iso15118EVCertificateStatusEnumType Status { get; set; }

      [Required]
      [MaxLength(5600)]
      public string ExiResponse { get; set; }

      public CustomDataType CustomData { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum Iso15118EVCertificateStatusEnumType
  {
      Accepted,
      Failed
  }

}