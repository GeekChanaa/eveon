using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class CustomDataType
  {
      [Required]
      [MaxLength(255)]
      public string VendorId { get; set; }
  }
}