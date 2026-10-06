using System;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  
  public class NotifyEVChargingNeedsRequest
  {
      public CustomDataType CustomData { get; set; }

      public int? MaxScheduleTuples { get; set; }

      [Required]
      public ChargingNeedsType ChargingNeeds { get; set; }

      [Required]
      public int EvseId { get; set; }
  }

  public class ChargingNeedsType
  {
      public CustomDataType CustomData { get; set; }

      public ACChargingParametersType ACChargingParameters { get; set; }

      public DCChargingParametersType DCChargingParameters { get; set; }

      [Required]
      public EnergyTransferModeEnumType RequestedEnergyTransfer { get; set; }

      [DataType(DataType.DateTime)]
      public DateTime? DepartureTime { get; set; }
  }

  public class ACChargingParametersType
  {
      [Required]
      public int EnergyAmount { get; set; }

      [Required]
      public int EvMinCurrent { get; set; }

      [Required]
      public int EvMaxCurrent { get; set; }

      [Required]
      public int EvMaxVoltage { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public class DCChargingParametersType
  {
      [Required]
      public int EvMaxCurrent { get; set; }

      [Required]
      public int EvMaxVoltage { get; set; }

      public int? EnergyAmount { get; set; }

      public int? EvMaxPower { get; set; }

      [Range(0, 100)]
      public int? StateOfCharge { get; set; }

      public int? EvEnergyCapacity { get; set; }

      [Range(0, 100)]
      public int? FullSoC { get; set; }

      [Range(0, 100)]
      public int? BulkSoC { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public enum EnergyTransferModeEnumType
  {
      DC,
      AC_single_phase,
      AC_two_phase,
      AC_three_phase
  }

}