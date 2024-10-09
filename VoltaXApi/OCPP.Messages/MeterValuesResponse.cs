using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class MeterValuesResponse
  {
      public CustomDataType? CustomData { get; set; }
  }

  public class CustomDataType
  {
      [Required]
      [MaxLength(255)]
      public string VendorId { get; set; }

      public IDictionary<string, object>? AdditionalProperties { get; set; }
  }

}