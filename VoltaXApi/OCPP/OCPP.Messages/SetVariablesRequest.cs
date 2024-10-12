using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetVariablesRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      [MinLength(1)]
      public List<SetVariableDataType> SetVariableData { get; set; }
  }

  public class SetVariableDataType
  {
      public CustomDataType? CustomData { get; set; }

      public AttributeEnumType? AttributeType { get; set; }

      [Required]
      [StringLength(1000)]
      public string AttributeValue { get; set; }

      [Required]
      public ComponentType Component { get; set; }

      [Required]
      public VariableType Variable { get; set; }
  }

}