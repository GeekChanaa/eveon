using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models.Ocpp201
{
    // Not IEntity: charger telemetry is append-only, no soft delete filter.
    /// <summary>One eventData entry of an OCPP 2.0.1 NotifyEvent.</summary>
    public class ChargerEvent
    {
        public long ID { get; set; }
        [MaxLength(64)]
        public string ChargePointID { get; set; } = "";
        public int EventId { get; set; }
        /// <summary>UTC time the event occurred on the charger.</summary>
        public DateTime Timestamp { get; set; }
        [MaxLength(16)]
        public string Trigger { get; set; } = "";
        [MaxLength(2500)]
        public string? ActualValue { get; set; }
        [MaxLength(50)]
        public string? TechCode { get; set; }
        [MaxLength(500)]
        public string? TechInfo { get; set; }
        public bool? Cleared { get; set; }
        public int? Cause { get; set; }
        [MaxLength(36)]
        public string? TransactionId { get; set; }
        [MaxLength(50)]
        public string ComponentName { get; set; } = "";
        [MaxLength(50)]
        public string? ComponentInstance { get; set; }
        [MaxLength(50)]
        public string VariableName { get; set; } = "";
        [MaxLength(50)]
        public string? VariableInstance { get; set; }
        public int? EvseId { get; set; }
        public int? ConnectorId { get; set; }
        public int? VariableMonitoringId { get; set; }
        [MaxLength(32)]
        public string EventNotificationType { get; set; } = "";
        /// <summary>Severity of the monitor that raised the event (0 = Danger ... 9 = Debug), when known.</summary>
        public int? Severity { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    /// <summary>A variable monitor reported by NotifyMonitoringReport (the charger's current monitoring setup).</summary>
    public class VariableMonitor
    {
        public long ID { get; set; }
        [MaxLength(64)]
        public string ChargePointID { get; set; } = "";
        [MaxLength(50)]
        public string ComponentName { get; set; } = "";
        [MaxLength(50)]
        public string? ComponentInstance { get; set; }
        [MaxLength(50)]
        public string VariableName { get; set; } = "";
        [MaxLength(50)]
        public string? VariableInstance { get; set; }
        public int? EvseId { get; set; }
        public int? ConnectorId { get; set; }
        public int MonitoringId { get; set; }
        [MaxLength(32)]
        public string Type { get; set; } = "";
        public double Value { get; set; }
        public int Severity { get; set; }
        public bool Transaction { get; set; }
        public int RequestId { get; set; }
        public DateTime ReportedAt { get; set; }
    }

    /// <summary>Customer data a charger returned for a CustomerInformation request, assembled from its NotifyCustomerInformation parts.</summary>
    public class CustomerInformationReport
    {
        public int ID { get; set; }
        [MaxLength(64)]
        public string ChargePointID { get; set; } = "";
        public int RequestId { get; set; }
        public bool Report { get; set; }
        public bool Clear { get; set; }
        [MaxLength(64)]
        public string? CustomerIdentifier { get; set; }
        [MaxLength(36)]
        public string? IdToken { get; set; }
        [MaxLength(32)]
        public string? CommandStatus { get; set; }
        public DateTime RequestedAt { get; set; }
        /// <summary>JSON object seqNo -> data part, kept until the last part arrives.</summary>
        public string? PartsJson { get; set; }
        public int PartsReceived { get; set; }
        /// <summary>The parts concatenated in seqNo order.</summary>
        public string? Data { get; set; }
        public bool Complete { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastPartAt { get; set; }
    }

    /// <summary>One display message reported by NotifyDisplayMessages for a GetDisplayMessages request (a snapshot per requestId).</summary>
    public class DisplayMessageSnapshot
    {
        public long ID { get; set; }
        [MaxLength(64)]
        public string ChargePointID { get; set; } = "";
        public int RequestId { get; set; }
        /// <summary>Null on the marker row of a request for which the charger reported no message.</summary>
        public int? MessageId { get; set; }
        [MaxLength(16)]
        public string? Priority { get; set; }
        [MaxLength(16)]
        public string? State { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        [MaxLength(36)]
        public string? TransactionId { get; set; }
        [MaxLength(1024)]
        public string? Content { get; set; }
        [MaxLength(8)]
        public string? Format { get; set; }
        [MaxLength(8)]
        public string? Language { get; set; }
        [MaxLength(50)]
        public string? DisplayComponentName { get; set; }
        [MaxLength(50)]
        public string? DisplayComponentInstance { get; set; }
        public int? DisplayEvseId { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    /// <summary>One-time upload URL handed to a charger for GetLog / GetDiagnostics. Only the SHA-256 of the token is stored.</summary>
    public class LogUploadTicket
    {
        public int ID { get; set; }
        [MaxLength(64)]
        public string TokenHash { get; set; } = "";
        [MaxLength(64)]
        public string ChargePointID { get; set; } = "";
        public int RequestId { get; set; }
        [MaxLength(32)]
        public string Purpose { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public int? CreatedByUserID { get; set; }
        /// <summary>Filename announced by the charger in its GetLog response.</summary>
        [MaxLength(255)]
        public string? AnnouncedFileName { get; set; }
        /// <summary>Last LogStatusNotification / DiagnosticsStatusNotification status.</summary>
        [MaxLength(32)]
        public string? Status { get; set; }
        public DateTime? StatusAt { get; set; }
    }

    /// <summary>A log file uploaded by a charger through a <see cref="LogUploadTicket"/>; stored outside wwwroot.</summary>
    public class UploadedChargerLog
    {
        public int ID { get; set; }
        public int LogUploadTicketID { get; set; }
        public LogUploadTicket? LogUploadTicket { get; set; }
        [MaxLength(64)]
        public string ChargePointID { get; set; } = "";
        [MaxLength(255)]
        public string FileName { get; set; } = "";
        /// <summary>Path relative to the charger-logs root ({chargePointId}/{guid}.{ext}).</summary>
        [MaxLength(300)]
        public string StoragePath { get; set; } = "";
        [MaxLength(100)]
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
        [MaxLength(64)]
        public string Sha256 { get; set; } = "";
        public DateTime UploadedAt { get; set; }
    }
}
