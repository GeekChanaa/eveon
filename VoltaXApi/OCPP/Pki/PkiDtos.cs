using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Pki
{
    /// <summary>A request the CSMS refuses before sending anything to the charger (HTTP 400).</summary>
    public sealed class PkiRequestException : Exception
    {
        public PkiRequestException(string message) : base(message) { }
    }

    public sealed record ChargerCertificateDto(int Id, string CertificateType, string SerialNumber, string Subject, string ThumbprintSha256,
        DateTime NotBefore, DateTime NotAfter, string Status, string? StatusReason, DateTime IssuedAt, DateTime? StatusChangedAt);

    public sealed record InstalledCertificateDto(int Id, string CertificateType, string HashAlgorithm, string IssuerNameHash,
        string IssuerKeyHash, string SerialNumber, DateTime ReportedAt, bool IsChargerCa);

    public sealed record ChargePointCertificatesDto(
        string ChargePointId,
        int SecurityProfile,
        bool HasPassword,
        bool HasPinnedCertificate,
        bool Connected,
        string? Protocol,
        bool CaConfigured,
        ChargerCertificateDto? Current,
        List<ChargerCertificateDto> Certificates,
        List<InstalledCertificateDto> Installed,
        DateTime? InstalledReportedAt);

    public sealed class InstallRootCertificateDto
    {
        public InstallCertificateUseEnumType CertificateType { get; set; } = InstallCertificateUseEnumType.CSMSRootCertificate;
        /// <summary>PEM to install; ignored when UseChargerCa is set.</summary>
        public string? CertificatePem { get; set; }
        /// <summary>Install the CSMS charger CA itself (only right as CSMSRootCertificate when the CSMS server certificate is issued by it).</summary>
        public bool UseChargerCa { get; set; }
    }

    public sealed class GetInstalledCertificateIdsDto
    {
        /// <summary>Empty or null: every type.</summary>
        public List<GetCertificateIdUseEnumType>? CertificateTypes { get; set; }
    }

    public sealed class SetSecurityProfileDto
    {
        public int SecurityProfile { get; set; }
        /// <summary>Change the CSMS-side profile even when the charger did not accept it (or is offline / OCPP 1.6).</summary>
        public bool Force { get; set; }
    }

    public sealed class RevokeChargerCertificateDto
    {
        public string? Reason { get; set; }
    }

    public sealed record SecurityProfileChangeResult(int RequestedProfile, int CsmsProfile, bool CsmsUpdated,
        string ChargerStatus, string? ChargerStatusInfo, List<string> Hints);
}
