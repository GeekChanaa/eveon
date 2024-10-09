namespace VoltaXApi.OCPP.Messages
{
  public class ChangeAvailabilityResponse
  {
      public CustomDataType CustomData { get; set; }
      public ChangeAvailabilityStatusEnumType Status { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }
  public enum ChangeAvailabilityStatusEnumType
  {
      Accepted,
      Rejected,
      Scheduled
  }

}