using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class StatusNotificationRequest
  {
      public CustomDataType? CustomData { get; set; }

      public string? Timestamp { get; set; }

      [Required]
      public ConnectorStatusEnumType ConnectorStatus { get; set; }

      [Required]
      public int EvseId { get; set; }

      [Required]
      public int ConnectorId { get; set; }
  }

  public enum ConnectorStatusEnumType
  {
      Available,
      Occupied,
      Reserved,
      Unavailable,
      Faulted
  }

}