using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
    public class NotifyEventRequest
    {
        public CustomDataType CustomData { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime GeneratedAt { get; set; }

        public bool Tbc { get; set; } = false; // Default value

        public int? SeqNo { get; set; } // Nullable if you want to allow omission

        [Required]
        [MinLength(1)]
        public List<EventDataType> EventData { get; set; }
    }

    public enum EventNotificationEnumType
    {
        HardWiredNotification,
        HardWiredMonitor,
        PreconfiguredMonitor,
        CustomMonitor
    }

    public enum EventTriggerEnumType
    {
        Alerting,
        Delta,
        Periodic
    }

    public class EventDataType
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime Timestamp { get; set; }

        [Required]
        public EventTriggerEnumType Trigger { get; set; }

        [Required]
        [StringLength(2500)]
        public string ActualValue { get; set; }

        [Required]
        public EventNotificationEnumType EventNotificationType { get; set; }

        [Required]
        public ComponentType Component { get; set; }

        [Required]
        public VariableType Variable { get; set; }

        public int? Cause { get; set; } // Nullable if you want to allow omission
        public string TechCode { get; set; }
        public string TechInfo { get; set; }
        public bool? Cleared { get; set; } // Nullable if you want to allow omission
        public string TransactionId { get; set; }
        public int? VariableMonitoringId { get; set; } // Nullable if you want to allow omission

        public CustomDataType CustomData { get; set; }
    }
}
