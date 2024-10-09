using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetVariablesResponse
  {
      public CustomDataType? customData { get; set; }

      [Required]
      [MinLength(1)]
      public List<SetVariableResultType> setVariableResult { get; set; } = new List<SetVariableResultType>();
  }

  public class SetVariableResultType
  {
      public CustomDataType? customData { get; set; }

      [Required]
      public AttributeEnumType? attributeType { get; set; }

      [Required]
      public SetVariableStatusEnumType? attributeStatus { get; set; }

      [Required]
      public StatusInfoType? attributeStatusInfo { get; set; }

      [Required]
      public ComponentType? component { get; set; }

      [Required]
      public VariableType? variable { get; set; }
  }

  public enum SetVariableStatusEnumType
  {
      Accepted,
      Rejected,
      UnknownComponent,
      UnknownVariable,
      NotSupportedAttributeType,
      RebootRequired
  }

}