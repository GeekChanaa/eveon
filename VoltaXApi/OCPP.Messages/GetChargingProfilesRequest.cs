
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetChargingProfilesRequest
  {
      [Required]
      public int RequestId { get; set; }

      public int? EvseId { get; set; }

      [Required]
      public ChargingProfileCriterionType ChargingProfile { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public class ChargingProfileCriterionType
  {
      public CustomDataType CustomData { get; set; }

      public ChargingProfilePurposeEnumType? ChargingProfilePurpose { get; set; }

      public int? StackLevel { get; set; }

      [MinLength(1)]
      public List<int> ChargingProfileId { get; set; }

      [MinLength(1)]
      [MaxLength(4)]
      public List<ChargingLimitSourceEnumType> ChargingLimitSource { get; set; }
  }

  public enum ChargingProfilePurposeEnumType
  {
      ChargingStationExternalConstraints,
      ChargingStationMaxProfile,
      TxDefaultProfile,
      TxProfile
  }

  public enum ChargingLimitSourceEnumType
  {
      EMS,
      Other,
      SO,
      CSO
  }

}