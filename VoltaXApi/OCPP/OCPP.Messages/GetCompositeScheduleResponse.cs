using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class GetCompositeScheduleResponse
  {
      [Required]
      public GenericStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }

      public CompositeScheduleType Schedule { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public class CompositeScheduleType
  {
      [Required]
      public int EvseId { get; set; }

      [Required]
      public int Duration { get; set; }

      [Required]
      [DataType(DataType.DateTime)]
      public DateTime ScheduleStart { get; set; }

      [Required]
      public ChargingRateUnitEnumType ChargingRateUnit { get; set; }

      [Required]
      [MinLength(1)]
      public List<ChargingSchedulePeriodType> ChargingSchedulePeriod { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}