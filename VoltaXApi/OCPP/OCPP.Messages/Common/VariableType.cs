

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class VariableType
  {
      [Required]
      [MaxLength(50)]
      public string Name { get; set; }

      public string? Instance { get; set; }
      public CustomDataType? CustomData { get; set; }
  }
}