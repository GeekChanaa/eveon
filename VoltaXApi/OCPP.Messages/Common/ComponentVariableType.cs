

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ComponentVariableType
  {
      [Required]
      public ComponentType Component { get; set; }

      public VariableType Variable { get; set; }

      public CustomDataType CustomData { get; set; }
  }
}