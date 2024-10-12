namespace VoltaXApi.OCPP.Messages
{
  public class CancelReservationResponse
  {
      public CustomDataType CustomData { get; set; }
      public CancelReservationStatusEnumType Status { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum CancelReservationStatusEnumType
  {
      Accepted,
      Rejected
  }

  

}