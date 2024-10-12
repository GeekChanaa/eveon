using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class GetVariablesRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MinLength(1)]
      public List<GetVariableDataType> GetVariableData { get; set; }
  }

  public class GetVariableDataType
  {
      [Required]
      public ComponentType Component { get; set; }

      [Required]
      public VariableType Variable { get; set; }

      public AttributeEnumType? AttributeType { get; set; }
  }

}