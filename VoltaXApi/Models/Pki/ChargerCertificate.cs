using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public enum ChargerCertificateType
    {
        ChargingStationCertificate,
        V2GCertificate
    }

    public enum ChargerCertificateStatus
    {
        /// <summary>Signed and sent with CertificateSigned, no answer yet.</summary>
        Pending,
        /// <summary>The charger accepted it; the current certificate of its type.</summary>
        Active,
        /// <summary>A newer certificate was accepted. Still trusted until it expires (the charger may not have switched yet).</summary>
        Replaced,
        /// <summary>Never trusted again for security profile 3.</summary>
        Revoked,
        /// <summary>The charger answered CertificateSigned with Rejected.</summary>
        Rejected,
        /// <summary>CertificateSigned could not be delivered (offline, timeout, CALLERROR).</summary>
        Failed
    }

    /// <summary>A client certificate the CSMS charger CA issued from a SignCertificate CSR.</summary>
    public class ChargerCertificate
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public ChargerCertificateType CertificateType { get; set; }
        [MaxLength(64)]
        public string SerialNumber { get; set; } = "";
        [MaxLength(512)]
        public string Subject { get; set; } = "";
        /// <summary>SHA-256 of the DER certificate, upper-case hex.</summary>
        [MaxLength(64)]
        public string ThumbprintSha256 { get; set; } = "";
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public string CertificatePem { get; set; } = "";
        public ChargerCertificateStatus Status { get; set; }
        [MaxLength(512)]
        public string? StatusReason { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime? StatusChangedAt { get; set; }
        public ChargePoint? ChargePoint { get; set; }
    }

    /// <summary>One certificate a charger reported with GetInstalledCertificateIds.</summary>
    public class InstalledCertificateRecord
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        /// <summary>GetCertificateIdUseEnumType name (CSMSRootCertificate, ManufacturerRootCertificate, V2GCertificateChain...).</summary>
        [MaxLength(32)]
        public string CertificateType { get; set; } = "";
        [MaxLength(8)]
        public string HashAlgorithm { get; set; } = "";
        [MaxLength(128)]
        public string IssuerNameHash { get; set; } = "";
        [MaxLength(128)]
        public string IssuerKeyHash { get; set; } = "";
        [MaxLength(40)]
        public string SerialNumber { get; set; } = "";
        /// <summary>childCertificateHashData of a V2G chain, as reported (JSON), if any.</summary>
        public string? ChildCertificatesJson { get; set; }
        public DateTime ReportedAt { get; set; }
        public ChargePoint? ChargePoint { get; set; }
    }

    /// <summary>The auto-created development charger CA. The private key is encrypted with Data Protection.</summary>
    public class PkiCertificateAuthority
    {
        public int ID { get; set; }
        [MaxLength(64)]
        public string Name { get; set; } = "";
        public string CertificatePem { get; set; } = "";
        public string PrivateKeyProtected { get; set; } = "";
        public DateTime NotAfter { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
