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
      Available,
      [System.Runtime.Serialization.EnumMember(Value = @"Occupied")]
      Occupied,
      [System.Runtime.Serialization.EnumMember(Value = @"Reserved")]
      Reserved,
      [System.Runtime.Serialization.EnumMember(Value = @"Unavailable")]
      Unavailable,
      [System.Runtime.Serialization.EnumMember(Value = @"Faulted")]
      Faulted
  }

}