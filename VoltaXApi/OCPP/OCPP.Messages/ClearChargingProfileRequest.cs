namespace VoltaXApi.OCPP.Messages
{
  /// <summary>OCPP 2.0.1 ClearChargingProfile.req: by id, or every profile matching the criteria (no criteria = all).</summary>
  public class ClearChargingProfileRequest
  {
      public CustomDataType? CustomData { get; set; }
      public int? ChargingProfileId { get; set; }
      public ClearChargingProfileType? ChargingProfileCriteria { get; set; }
  }

  public class ClearChargingProfileType
  {
      public CustomDataType? CustomData { get; set; }
      /// <summary>0 = profiles of the whole charging station.</summary>
      public int? EvseId { get; set; }
      public ChargingProfilePurposeEnumType? ChargingProfilePurpose { get; set; }
      public int? StackLevel { get; set; }
  }
}
