using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class NotifyChargingLimitRequest
  {
      public CustomDataType CustomData { get; set; }
      public ChargingLimitType ChargingLimit { get; set; }
      public List<ChargingScheduleType> ChargingSchedule { get; set; }
      public int EvseId { get; set; }
  }
  public class ChargingLimitType
  {
      [Required]
      public ChargingLimitSourceEnumType ChargingLimitSource { get; set; }
      public CustomDataType CustomData { get; set; }
      public bool? IsGridCritical { get; set; }
  }

  public class RelativeTimeIntervalType
  {
      [Required]
      public int Start { get; set; }
      public int? Duration { get; set; }
      public CustomDataType CustomData { get; set; }
  }

  public class SalesTariffEntryType
  {
      [Required]
      public RelativeTimeIntervalType RelativeTimeInterval { get; set; }
      public int? EPriceLevel { get; set; }
      [Required]
      public List<ConsumptionCostType> ConsumptionCost { get; set; }
      public CustomDataType CustomData { get; set; }
  }

}