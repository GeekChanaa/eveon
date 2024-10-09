using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ReservationStatusUpdateRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public int ReservationId { get; set; }

      [Required]
      public ReservationUpdateStatusEnumType ReservationUpdateStatus { get; set; }
  }
  
  public enum ReservationUpdateStatusEnumType
  {
      Expired,
      Removed
  }

}