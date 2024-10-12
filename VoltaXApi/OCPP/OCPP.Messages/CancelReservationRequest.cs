namespace VoltaXApi.OCPP.Messages
{
  public class CancelReservationRequest
  {
      public CustomDataType CustomData { get; set; }
      public int ReservationId { get; set; }
  }
}