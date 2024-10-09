namespace VoltaXApi.OCPP.Messages
{
  public class BootNotificationResponse
  {
      public CustomDataType CustomData { get; set; }
      public DateTime CurrentTime { get; set; }
      public int Interval { get; set; }
      public RegistrationStatusEnumType Status { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum RegistrationStatusEnumType
  {
      Accepted,
      Pending,
      Rejected
  }

}