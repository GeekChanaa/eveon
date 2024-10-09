using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  
  public class GetVariablesResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MinLength(1)]
      public List<GetVariableResultType> GetVariableResult { get; set; }
  }

  public enum GetVariableStatusEnumType
  {
      Accepted,
      Rejected,
      UnknownComponent,
      UnknownVariable,
      NotSupportedAttributeType
  }

  public class GetVariableResultType
  {
      [Required]
      public GetVariableStatusEnumType AttributeStatus { get; set; }

      [Required]
      public ComponentType Component { get; set; }

      [Required]
      public VariableType Variable { get; set; }

      public CustomDataType CustomData { get; set; }
      
      public AttributeEnumType? AttributeType { get; set; }

      [MaxLength(2500)]
      public string AttributeValue { get; set; }

      public StatusInfoType AttributeStatusInfo { get; set; }
  }

}