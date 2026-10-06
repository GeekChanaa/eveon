using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.OCPP.Pki
{
    public interface IChargerCertificateService
    {
        /// <summary>Signs the CSR with the charger CA, stores the certificate and delivers it with CertificateSigned.</summary>
        Task<ChargerCertificate> IssueAndSendAsync(string chargePointId, string csr, CertificateSigningUseEnumType certificateType, CancellationToken cancellationToken = default);
    }

    public sealed class ChargerCertificateService : IChargerCertificateService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly IChargerCertificateAuthority _ca;
        private readonly IOcppCommandSender _commandSender;
        private readonly IAuditLogger _audit;
        private readonly PkiSettings _settings;
        private readonly ILogger<ChargerCertificateService> _logger;

        public ChargerCertificateService(VoltaXApiDbContext db, IChargerCertificateAuthority ca, IOcppCommandSender commandSender,
            IAuditLogger audit, IConfiguration configuration, ILogger<ChargerCertificateService> logger)
        {
            _db = db;
            _ca = ca;
            _commandSender = commandSender;
            _audit = audit;
            _settings = PkiSettings.From(configuration);
            _logger = logger;
        }

        public async Task<ChargerCertificate> IssueAndSendAsync(string chargePointId, string csr, CertificateSigningUseEnumType certificateType, CancellationToken cancellationToken = default)
        {
            var ca = await _ca.GetCaAsync(cancellationToken) ?? throw new InvalidOperationException("No charger CA is configured.");
            var validation = ChargerPkiCrypto.ValidateCsr(csr, chargePointId);
            if (!validation.IsValid) throw new InvalidOperationException("Invalid CSR: " + validation.Error);
            var chargePointKey = await _db.ChargePoints.AsNoTracking().Where(cp => cp.ChargePointId == chargePointId)
                .Select(cp => (int?)cp.ID).FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Unknown charge point {chargePointId}.");

            var now = DateTime.UtcNow;
            using var leaf = ChargerPkiCrypto.IssueChargerCertificate(validation.Request!, ca, _settings.ChargerCertificateDays, now);
            var type = certificateType == CertificateSigningUseEnumType.V2GCertificate ? ChargerCertificateType.V2GCertificate : ChargerCertificateType.ChargingStationCertificate;
            var issued = new ChargerCertificate
            {
                ChargePointID = chargePointKey,
                CertificateType = type,
                SerialNumber = leaf.SerialNumber,
                Subject = leaf.Subject,
                ThumbprintSha256 = ChargerPkiCrypto.Sha256Thumbprint(leaf),
                NotBefore = leaf.NotBefore.ToUniversalTime(),
                NotAfter = leaf.NotAfter.ToUniversalTime(),
                CertificatePem = ChargerPkiCrypto.ToPem(leaf),
                Status = ChargerCertificateStatus.Pending,
                IssuedAt = now
            };
            _db.ChargerCertificates.Add(issued);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("PKI: issued certificate {Serial} for {ChargePointId} ({Type}), valid until {NotAfter:o}",
                issued.SerialNumber, chargePointId, type, issued.NotAfter);

            CertificateSignedResponse response;
            try
            {
                response = await _commandSender.SendRequestAsync<CertificateSignedRequest, CertificateSignedResponse>(chargePointId, "CertificateSigned",
                    new CertificateSignedRequest { CertificateChain = issued.CertificatePem + ChargerPkiCrypto.ToPem(ca), CertificateType = certificateType },
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException or WebSocketNotFoundException)
            {
                await SetStatus(issued, ChargerCertificateStatus.Failed, "Not delivered: " + ex.Message, cancellationToken);
                throw;
            }

            if (response.Status == CertificateSignedStatusEnumType.Accepted)
            {
                var previous = await _db.ChargerCertificates
                    .Where(c => c.ChargePointID == chargePointKey && c.CertificateType == type && c.Status == ChargerCertificateStatus.Active && c.ID != issued.ID)
                    .ToListAsync(cancellationToken);
                foreach (var old in previous)
                {
                    old.Status = ChargerCertificateStatus.Replaced;
                    old.StatusReason = $"Replaced by {issued.SerialNumber}";
                    old.StatusChangedAt = now;
                }
                await SetStatus(issued, ChargerCertificateStatus.Active, null, cancellationToken);
                _logger.LogInformation("PKI: {ChargePointId} accepted certificate {Serial}; {Count} previous certificate(s) replaced", chargePointId, issued.SerialNumber, previous.Count);
            }
            else
            {
                var reason = response.StatusInfo == null ? "Rejected by the charger" : $"Rejected by the charger: {response.StatusInfo.ReasonCode} {response.StatusInfo.AdditionalInfo}".Trim();
                await SetStatus(issued, ChargerCertificateStatus.Rejected, reason, cancellationToken);
                _logger.LogWarning("PKI: {ChargePointId} rejected certificate {Serial}: {Reason}", chargePointId, issued.SerialNumber, reason);
            }
            await _audit.LogAsync("Pki.CertificateSigned", "ChargePoint", chargePointId,
                new { issued.SerialNumber, issued.ThumbprintSha256, issued.NotAfter, Status = issued.Status.ToString() });
            return issued;
        }

        private async Task SetStatus(ChargerCertificate certificate, ChargerCertificateStatus status, string? reason, CancellationToken cancellationToken)
        {
            certificate.Status = status;
            certificate.StatusReason = reason?.Length > 512 ? reason[..512] : reason;
            certificate.StatusChangedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
