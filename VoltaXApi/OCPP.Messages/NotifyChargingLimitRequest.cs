using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class NotifyChargingLimitRequest
  {
      public CustomDataType CustomData { get; set; }
      public ChargingLimitType ChargingLimit { get; set; }
  }

  public enum ChargingLimitSourceEnumType
  {
      EMS,
      Other,
      SO,
      CSO
  }
  public class ChargingLimitType
  {
      [Required]
      public ChargingLimitSourceEnumType ChargingLimitSource { get; set; }
      public CustomDataType CustomData { get; set; }
      public bool? IsGridCritical { get; set; }
  }

  public class ChargingSchedulePeriodType
  {
      [Required]
      public int StartPeriod { get; set; }
      [Required]
      public double Limit { get; set; }
      public CustomDataType CustomData { get; set; }
      public int? NumberPhases { get; set; }
      public int? PhaseToUse { get; set; }
  }

  public class ChargingScheduleType
  {
      [Required]
      public int Id { get; set; }
      [Required]
      public ChargingRateUnitEnumType ChargingRateUnit { get; set; }
      [Required]
      public List<ChargingSchedulePeriodType> ChargingSchedulePeriod { get; set; }
      public CustomDataType CustomData { get; set; }
      public DateTime? StartSchedule { get; set; }
      public int? Duration { get; set; }
      public double? MinChargingRate { get; set; }
      public SalesTariffType SalesTariff { get; set; }
  }

  public class ConsumptionCostType
  {
      [Required]
      public double StartValue { get; set; }
      [Required]
      public List<CostType> Cost { get; set; }
      public CustomDataType CustomData { get; set; }
  }

  public class CostType
  {
      [Required]
      public CostKindEnumType CostKind { get; set; }
      [Required]
      public int Amount { get; set; }
      public int? AmountMultiplier { get; set; }
      public CustomDataType CustomData { get; set; }
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

  public class SalesTariffType
  {
      public List<SalesTariffEntryType> SalesTariffEntries { get; set; }
  }

}