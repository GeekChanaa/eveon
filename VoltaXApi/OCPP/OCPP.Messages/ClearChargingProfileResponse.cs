namespace VoltaXApi.OCPP.Messages
{
  public class ClearChargingProfileResponse
  {
      public CustomDataType CustomData { get; set; }
      public ClearChargingProfileStatusEnum Status { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum ClearChargingProfileStatusEnum
  {
      Accepted,
      Unknown
  }

}