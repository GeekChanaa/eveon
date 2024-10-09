using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetCertificateStatusResponse
  {
      [Required]
      public GetCertificateStatusEnumType Status { get; set; }

      public CustomDataType CustomData { get; set; }

      public StatusInfoType StatusInfo { get; set; }

      [MaxLength(5500)]
      public string OcspResult { get; set; }
  }

  public enum GetCertificateStatusEnumType
  {
      Accepted,
      Failed
  }

}