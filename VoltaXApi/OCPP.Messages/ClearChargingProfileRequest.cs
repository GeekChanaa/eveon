namespace VoltaXApi.OCPP.Messages
{
    public class ClearChargingProfileRequest
  {
      public CustomDataType CustomData { get; set; }
      public int ChargingProfileId { get; set; }
      public ClearChargingProfileType ChargingProfileCriteria { get; set; }
  }

  public enum ChargingProfilePurposeEnum
  {
      ChargingStationExternalConstraints,
      ChargingStationMaxProfile,
      TxDefaultProfile,
      TxProfile
  }

  public class ClearChargingProfileType
  {
      public CustomDataType CustomData { get; set; }
      public int? EvseId { get; set; } // Nullable integer for EVSE ID, 0 specifies overall Charging Station
      public ChargingProfilePurposeEnum ChargingProfilePurpose { get; set; }
      public int? StackLevel { get; set; } // Nullable integer for stack level
  }

}