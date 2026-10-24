using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models.Alarms
{
    /// <summary>What raised an alarm.</summary>
    public enum AlarmType
    {
        /// <summary>A StatusNotification reported a fault (1.6 errorCode other than NoError or status Faulted, 2.0.1 Faulted).</summary>
        ConnectorFault,
        /// <summary>An OCPP 2.0.1 NotifyEvent of an alarm-worthy monitor or fault variable.</summary>
        DeviceEvent,
        /// <summary>The charger stayed disconnected longer than <c>Alarms:OfflineAfterMinutes</c>.</summary>
        Offline,
        /// <summary>The charger booted <c>Alarms:BootLoopCount</c> times within <c>Alarms:BootLoopWindowMinutes</c>.</summary>
        BootLoop
    }

    /// <summary>Ordered: a higher value is more severe.</summary>
    public enum AlarmSeverity
    {
        Warning = 0,
        Minor = 1,
        Major = 2,
        Critical = 3
    }

    public enum AlarmStatus
    {
        /// <summary>Active, nobody took it yet.</summary>
        Open,
        /// <summary>Active, an operator is on it.</summary>
        Acknowledged,
        /// <summary>Closed by an operator (with a comment).</summary>
        Resolved,
        /// <summary>Closed automatically: the charger reported the fault gone.</summary>
        Cleared
    }

    /// <summary>
    /// An operational alarm of a charge point. One alarm per fault while it is active (Open or Acknowledged): the same
    /// fault reported again raises <see cref="Count"/> and <see cref="LastSeenAt"/> instead of creating another row.
    /// <see cref="OpenKey"/> enforces that with a unique index: it equals <see cref="DedupKey"/> while the alarm is
    /// active and gets the alarm id appended once it is closed.
    /// </summary>
    public class Alarm
    {
        public long ID { get; set; }

        public AlarmType Type { get; set; }
        /// <summary>Fault code: the 1.6 error code, "Faulted", "Component.Variable" of a device event, "Offline", "BootLoop".</summary>
        [MaxLength(100)]
        public string Code { get; set; } = "";
        public AlarmSeverity Severity { get; set; }
        public AlarmStatus Status { get; set; }
        [MaxLength(500)]
        public string Description { get; set; } = "";
        /// <summary>Extra information from the charger (info text, vendor error code, tech code...).</summary>
        [MaxLength(1000)]
        public string? Details { get; set; }

        public int ChargePointID { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        /// <summary>The OCPP identity of the charge point (kept so closed alarms stay readable).</summary>
        [MaxLength(64)]
        public string ChargePointIdentity { get; set; } = "";
        /// <summary>OCPP EVSE number (1.6: the connector id); null for the charger as a whole.</summary>
        public int? EvseId { get; set; }
        /// <summary>OCPP connector number within the EVSE; null for the whole EVSE or charger.</summary>
        public int? ConnectorNumber { get; set; }
        /// <summary>Our connector row when it is known.</summary>
        public int? ConnectorID { get; set; }

        public DateTime FirstSeenAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public int Count { get; set; } = 1;

        public DateTime? AcknowledgedAt { get; set; }
        public int? AcknowledgedByID { get; set; }
        public User? AcknowledgedBy { get; set; }
        public DateTime? AssignedAt { get; set; }
        public int? AssignedToID { get; set; }
        public User? AssignedTo { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByID { get; set; }
        public User? ResolvedBy { get; set; }
        [MaxLength(1000)]
        public string? ResolutionComment { get; set; }
        public DateTime? ClearedAt { get; set; }

        [MaxLength(200)]
        public string DedupKey { get; set; } = "";
        [MaxLength(230)]
        public string OpenKey { get; set; } = "";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsActive => Status is AlarmStatus.Open or AlarmStatus.Acknowledged;

        public static string KeyOf(AlarmType type, int chargePointId, int? evseId, int? connectorNumber, string code) =>
            $"{type}|{chargePointId}|{evseId}|{connectorNumber}|{code}";

        /// <summary>Closes the alarm: frees its dedup key for a new occurrence of the same fault.</summary>
        public void Close(AlarmStatus status, DateTime at)
        {
            Status = status;
            OpenKey = $"{DedupKey}#{ID}";
            UpdatedAt = at;
        }
    }
}
