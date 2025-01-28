using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class GetReportRequest
  {
      public CustomDataType? CustomData { get; set; }

      [MinLength(1)]
      public List<ComponentVariableType>? ComponentVariable { get; set; }

      public int RequestId { get; set; }

      public List<ComponentCriterionEnumType>? ComponentCriteria { get; set; }
  }

  public enum ComponentCriterionEnumType
  {
      Active,
      Available,
      Enabled,
      Problem
  }
}