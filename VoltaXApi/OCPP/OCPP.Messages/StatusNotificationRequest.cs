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
    [System.Runtime.Serialization.EnumMember(Value = @"Available")]
    Available = 0,
    [System.Runtime.Serialization.EnumMember(Value = @"Occupied")]
    Occupied = 1,
    [System.Runtime.Serialization.EnumMember(Value = @"Reserved")]
    Reserved = 2,
    [System.Runtime.Serialization.EnumMember(Value = @"Unavailable")]
    Unavailable = 3,
    [System.Runtime.Serialization.EnumMember(Value = @"Faulted")]
    Faulted = 4,
    [System.Runtime.Serialization.EnumMember(Value = @"Disconnected")]
    Disconnected = 5
  }

}