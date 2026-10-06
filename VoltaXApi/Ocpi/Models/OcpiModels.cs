using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Ocpi.Models
{
    public enum OcpiPartyStatus
    {
        // Token A generated (or our registration started), handshake not finished.
        Pending,
        Registered,
        Suspended,
        Unregistered
    }

    // Deliberately not IEntity: no soft delete filter, rows are kept for the audit trail.
    public class OcpiParty
    {
        public int ID { get; set; }
        [MaxLength(100)]
        public string Name { get; set; } = "";
        [MaxLength(2)]
        public string? CountryCode { get; set; }
        [MaxLength(3)]
        public string? PartyId { get; set; }
        [MaxLength(16)]
        public string? Role { get; set; }
        // Every role the partner declared in its credentials (hubs declare several).
        public string? RolesJson { get; set; }
        public OcpiPartyStatus Status { get; set; }
        [MaxLength(512)]
        public string? VersionsUrl { get; set; }
        [MaxLength(10)]
        public string? Version { get; set; }
        public string? EndpointsJson { get; set; }
        // SHA-256 of the token A we generated for them; cleared once the handshake is done.
        [MaxLength(64)]
        public string? TokenAHash { get; set; }
        // SHA-256 of the token they use to call us (TOKEN_B or TOKEN_C).
        [MaxLength(64)]
        public string? IncomingTokenHash { get; set; }
        // Data Protection payload of the token we use to call them.
        public string? OutgoingTokenProtected { get; set; }
        [MaxLength(1024)]
        public string? LastError { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Token pushed by an eMSP through the Tokens receiver interface.
    public class OcpiToken
    {
        public int ID { get; set; }
        public int OcpiPartyID { get; set; }
        public OcpiParty? OcpiParty { get; set; }
        [MaxLength(2)]
        public string CountryCode { get; set; } = "";
        [MaxLength(3)]
        public string PartyId { get; set; } = "";
        [MaxLength(36)]
        public string Uid { get; set; } = "";
        [MaxLength(16)]
        public string Type { get; set; } = "RFID";
        [MaxLength(36)]
        public string ContractId { get; set; } = "";
        [MaxLength(64)]
        public string? VisualNumber { get; set; }
        [MaxLength(64)]
        public string Issuer { get; set; } = "";
        [MaxLength(36)]
        public string? GroupId { get; set; }
        public bool Valid { get; set; }
        [MaxLength(16)]
        public string Whitelist { get; set; } = "ALLOWED";
        [MaxLength(2)]
        public string? Language { get; set; }
        [MaxLength(16)]
        public string? DefaultProfileType { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum OcpiSessionStatus
    {
        Pending,
        Active,
        Completed,
        Invalid
    }

    // A charging session started with a roaming token. Source of the Sessions and CDRs modules.
    public class OcpiSession
    {
        public int ID { get; set; }
        public int OcpiPartyID { get; set; }
        public OcpiParty? OcpiParty { get; set; }
        [MaxLength(2)]
        public string TokenCountryCode { get; set; } = "";
        [MaxLength(3)]
        public string TokenPartyId { get; set; } = "";
        [MaxLength(36)]
        public string TokenUid { get; set; } = "";
        [MaxLength(16)]
        public string TokenType { get; set; } = "RFID";
        [MaxLength(36)]
        public string ContractId { get; set; } = "";
        [MaxLength(16)]
        public string AuthMethod { get; set; } = "WHITELIST";
        [MaxLength(36)]
        public string? AuthorizationReference { get; set; }
        public int ChargingStationID { get; set; }
        public int ChargePointID { get; set; }
        public int EvseId { get; set; }
        public int? ConnectorID { get; set; }
        [MaxLength(36)]
        public string? TransactionUid { get; set; }
        public OcpiSessionStatus Status { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public double? MeterStartKwh { get; set; }
        public double Kwh { get; set; }
        // Tariff snapshot taken at start (prices include VAT, as in the rest of EVEON).
        public double PricePerKWh { get; set; }
        public double PricePerMinute { get; set; }
        public double PricePerIdleMinute { get; set; }
        public double FlatFee { get; set; }
        public double VatRate { get; set; }
        public DateTime? CdrQueuedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // OCPI reservation ids are strings; the row ID is the integer id sent to the charger.
    public class OcpiReservation
    {
        public int ID { get; set; }
        public int OcpiPartyID { get; set; }
        [MaxLength(36)]
        public string ReservationId { get; set; } = "";
        [MaxLength(36)]
        public string TokenUid { get; set; } = "";
        public int ChargePointID { get; set; }
        public int? EvseId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool Cancelled { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum OcpiOutboxStatus
    {
        Pending,
        Sent,
        Failed
    }

    // Outgoing calls to partners (push of locations, sessions, CDRs, command results).
    public class OcpiOutboxMessage
    {
        public long ID { get; set; }
        public int OcpiPartyID { get; set; }
        public OcpiParty? OcpiParty { get; set; }
        [MaxLength(16)]
        public string Module { get; set; } = "";
        [MaxLength(8)]
        public string Method { get; set; } = "POST";
        [MaxLength(1024)]
        public string Url { get; set; } = "";
        public string? PayloadJson { get; set; }
        public OcpiOutboxStatus Status { get; set; }
        public int Attempts { get; set; }
        public DateTime NextAttemptAt { get; set; }
        [MaxLength(1024)]
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }

    // Change-scan watermarks of the push worker, persisted so a restart does not lose changes.
    public class OcpiSyncCursor
    {
        [Key]
        [MaxLength(64)]
        public string Name { get; set; } = "";
        public DateTime Position { get; set; }
    }
}
