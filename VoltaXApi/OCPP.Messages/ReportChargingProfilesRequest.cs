using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ReportChargingProfilesRequest
  {
      public List<ChargingProfileType> ChargingProfiles { get; set; }
  }
  public enum ChargingLimitSourceEnumType
  {
      EMS,
      Other,
      SO,
      CSO
  }

  public enum ChargingProfileKindEnumType
  {
      Absolute,
      Recurring,
      Relative
  }

  public enum ChargingProfilePurposeEnumType
  {
      ChargingStationExternalConstraints,
      ChargingStationMaxProfile,
      TxDefaultProfile,
      TxProfile
  }

  public enum RecurrencyKindEnumType
  {
      Daily,
      Weekly
  }

  public class ChargingProfileType
  {
      [Required]
      public int Id { get; set; }

      [Required]
      public int StackLevel { get; set; }

      [Required]
      public ChargingProfilePurposeEnumType ChargingProfilePurpose { get; set; }

      [Required]
      public ChargingProfileKindEnumType ChargingProfileKind { get; set; }

      public RecurrencyKindEnumType? RecurrencyKind { get; set; }

      public DateTime? ValidFrom { get; set; }

      public DateTime? ValidTo { get; set; }

      [Required]
      [MinLength(1)]
      [MaxLength(3)]
      public List<ChargingScheduleType> ChargingSchedule { get; set; }

      [MaxLength(36)]
      public string TransactionId { get; set; }
  }

  public class ChargingScheduleType
  {
      public int Id { get; set; }

      public DateTime? StartSchedule { get; set; }

      public int? Duration { get; set; }

      [Required]
      public ChargingRateUnitEnumType ChargingRateUnit { get; set; }

      [Required]
      [MinLength(1)]
      [MaxLength(1024)]
      public List<ChargingSchedulePeriodType> ChargingSchedulePeriod { get; set; }

      public double? MinChargingRate { get; set; }

      public SalesTariffType SalesTariff { get; set; }
  }

  public class ConsumptionCostType
  {
      [Required]
      public double StartValue { get; set; }

      [Required]
      [MinLength(1)]
      [MaxLength(3)]
      public List<CostType> Cost { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public class CostType
  {
      [Required]
      public CostKindEnumType CostKind { get; set; }

      [Required]
      public double Amount { get; set; }
  }

  public class SalesTariffType
  {
      [Required]
      public int Id { get; set; }

      [Required]
      public string Name { get; set; }

      public List<ConsumptionCostType> ConsumptionCosts { get; set; }

      public double? FixedCost { get; set; }

      public double? VariableCost { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}