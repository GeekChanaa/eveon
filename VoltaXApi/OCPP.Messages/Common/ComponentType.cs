

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ComponentType
  {
      [Required]
      [MaxLength(50)]
      public string Name { get; set; }

      public EVSEType Evse { get; set; }
      public string Instance { get; set; }
      public CustomDataType CustomData { get; set; }
  }
}