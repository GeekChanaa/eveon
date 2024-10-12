using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class DeleteCertificateResponse
  {
      public CustomDataType CustomData { get; set; }
      
      [Required]
      public DeleteCertificateStatusEnumType Status { get; set; }
      
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum DeleteCertificateStatusEnumType
  {
      Accepted,
      Failed,
      NotFound
  }

}