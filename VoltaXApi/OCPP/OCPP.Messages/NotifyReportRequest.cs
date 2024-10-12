using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  
  public class NotifyReportRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public int RequestId { get; set; }

      [Required]
      [DataType(DataType.DateTime)]
      public DateTime GeneratedAt { get; set; }

      [Required]
      public List<ReportDataType> ReportData { get; set; }

      public bool? Tbc { get; set; } = false; // Default is false

      public int? SeqNo { get; set; }
  }

  public class ReportDataType
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public ComponentType Component { get; set; }

      [Required]
      public VariableType Variable { get; set; }

      [Required]
      [MinLength(1), MaxLength(4)] // Enforce min and max items in array
      public List<VariableAttributeType> VariableAttribute { get; set; }

      public VariableCharacteristicsType VariableCharacteristics { get; set; }
  }

  public class VariableAttributeType
  {
      public CustomDataType CustomData { get; set; }

      public AttributeEnumType Type { get; set; }

      [MaxLength(2500)]
      public string Value { get; set; }

      public MutabilityEnumType? Mutability { get; set; }

      public bool? Persistent { get; set; } = false; // Default is false

      public bool? Constant { get; set; } = false; // Default is false
  }

  public class VariableCharacteristicsType
  {
      public CustomDataType CustomData { get; set; }

      [MaxLength(16)]
      public string Unit { get; set; }

      public double? MinLimit { get; set; }

      public double? MaxLimit { get; set; }

      [MaxLength(1000)]
      public string ValuesList { get; set; }

      [Required]
      public DataEnumType DataType { get; set; }

      [Required]
      public bool SupportsMonitoring { get; set; }
  }

  public enum DataEnumType
  {
      String,
      Decimal,
      Integer,
      DateTime,
      Boolean,
      OptionList,
      SequenceList,
      MemberList
  }

  public enum MutabilityEnumType
  {
      ReadOnly,
      WriteOnly,
      ReadWrite
  }

}