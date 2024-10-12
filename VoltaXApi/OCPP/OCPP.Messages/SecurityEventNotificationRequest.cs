using System;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SecurityEventNotificationRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MaxLength(50)]
      public string Type { get; set; }  // Type of the security event

      [Required]
      public DateTime Timestamp { get; set; }  // Date and time of the event

      [MaxLength(255)]
      public string TechInfo { get; set; }  // Additional information about the event
  }

}