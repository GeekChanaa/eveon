using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Pki;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Security and certificate management commands (OCPP 2.0.1, blocks A and M). Every command waits for the
    /// charger's answer (see <see cref="IOcppCommandSender"/>); PKI state is kept in ChargerCertificates and
    /// InstalledCertificateRecords.
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly IOcppCommandSender _commandSender;
        private readonly IConfigurationService _configurationService;
        private readonly VoltaXApiDbContext _db;
        private readonly IChargerCertificateAuthority _ca;
        private readonly IAuditLogger _audit;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(IOcppCommandSender commandSender, IConfigurationService configurationService, VoltaXApiDbContext db,
            IChargerCertificateAuthority ca, IAuditLogger audit, ILogger<SecurityService> logger)
        {
            _commandSender = commandSender;
            _configurationService = configurationService;
            _db = db;
            _ca = ca;
            _audit = audit;
            _logger = logger;
        }

        public async Task<InstallCertificateResponse> InstallCertificate(string chargePointID, InstallCertificateRequest request, CancellationToken cancellationToken = default)
        {
            EnsureOcpp201(chargePointID);
            var response = await _commandSender.SendRequestAsync<InstallCertificateRequest, InstallCertificateResponse>(chargePointID, "InstallCertificate", request, cancellationToken: cancellationToken);
            _logger.LogInformation("InstallCertificate ({Type}) answered by {ChargePointId}: {Status}", request.CertificateType, chargePointID, response.Status);
            return response;
        }

        public async Task<InstallCertificateResponse> InstallRootCertificate(string chargePointID, InstallRootCertificateDto request, CancellationToken cancellationToken = default)
        {
            if (request.CertificateType is not (InstallCertificateUseEnumType.CSMSRootCertificate or InstallCertificateUseEnumType.ManufacturerRootCertificate))
                throw new PkiRequestException("Only CSMSRootCertificate and ManufacturerRootCertificate can be installed from here.");

            string pem;
            if (request.UseChargerCa)
            {
                var ca = await _ca.GetCaAsync(cancellationToken) ?? throw new PkiRequestException("No charger CA is configured.");
                pem = ChargerPkiCrypto.ToPem(ca);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.CertificatePem)) throw new PkiRequestException("Paste the PEM certificate to install.");
                System.Security.Cryptography.X509Certificates.X509Certificate2 parsed;
                try
                {
                    parsed = ChargerPkiCrypto.ParseCertificate(request.CertificatePem);
                }
                catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException)
                {
                    throw new PkiRequestException("The certificate could not be read (PEM or base64 DER expected).");
                }
                using (parsed)
                {
                    var constraints = parsed.Extensions.OfType<System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension>().FirstOrDefault();
                    if (constraints == null || !constraints.CertificateAuthority)
                        throw new PkiRequestException("The certificate is not a CA certificate (basicConstraints cA=true).");
                    pem = ChargerPkiCrypto.ToPem(parsed);
                }
            }
            if (pem.Length > 5500) throw new PkiRequestException("The certificate is longer than the 5500 characters OCPP allows.");

            var response = await InstallCertificate(chargePointID, new InstallCertificateRequest { CertificateType = request.CertificateType, Certificate = pem }, cancellationToken);
            await _audit.LogAsync("Pki.InstallCertificate", "ChargePoint", chargePointID,
                new { CertificateType = request.CertificateType.ToString(), request.UseChargerCa, Status = response.Status.ToString() });
            return response;
        }

        public async Task<DeleteCertificateResponse> DeleteCertificate(string chargePointID, CertificateHashDataType hashData, CancellationToken cancellationToken = default)
        {
            EnsureOcpp201(chargePointID);
            if (hashData == null || string.IsNullOrWhiteSpace(hashData.IssuerNameHash) || string.IsNullOrWhiteSpace(hashData.IssuerKeyHash) || string.IsNullOrWhiteSpace(hashData.SerialNumber))
                throw new PkiRequestException("hashAlgorithm, issuerNameHash, issuerKeyHash and serialNumber are required.");

            var response = await _commandSender.SendRequestAsync<DeleteCertificateRequest, DeleteCertificateResponse>(chargePointID, "DeleteCertificate",
                new DeleteCertificateRequest { CertificateHashData = hashData }, cancellationToken: cancellationToken);
            _logger.LogInformation("DeleteCertificate {Serial} answered by {ChargePointId}: {Status}", hashData.SerialNumber, chargePointID, response.Status);

            if (response.Status is DeleteCertificateStatusEnumType.Accepted or DeleteCertificateStatusEnumType.NotFound)
            {
                var chargePointKey = await ChargePointKey(chargePointID, cancellationToken);
                if (chargePointKey != null)
                {
                    var records = await _db.InstalledCertificateRecords.Where(r => r.ChargePointID == chargePointKey).ToListAsync(cancellationToken);
                    _db.InstalledCertificateRecords.RemoveRange(records.Where(r =>
                        ChargerPkiCrypto.HashDataMatches(hashData, r.HashAlgorithm, r.IssuerNameHash, r.IssuerKeyHash, r.SerialNumber)));
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            await _audit.LogAsync("Pki.DeleteCertificate", "ChargePoint", chargePointID, new { hashData.SerialNumber, Status = response.Status.ToString() });
            return response;
        }

        public async Task<GetInstalledCertificateIdsResponse> GetInstalledCertificateIds(string chargePointID, List<GetCertificateIdUseEnumType>? certificateTypes, CancellationToken cancellationToken = default)
        {
            EnsureOcpp201(chargePointID);
            var types = certificateTypes?.Distinct().ToList();
            if (types is { Count: 0 }) types = null;
            var response = await _commandSender.SendRequestAsync<GetInstalledCertificateIdsRequest, GetInstalledCertificateIdsResponse>(chargePointID, "GetInstalledCertificateIds",
                new GetInstalledCertificateIdsRequest { CertificateType = types! }, cancellationToken: cancellationToken);

            var chargePointKey = await ChargePointKey(chargePointID, cancellationToken);
            if (chargePointKey != null)
            {
                var typeNames = types?.Select(t => t.ToString()).ToList();
                var query = _db.InstalledCertificateRecords.Where(r => r.ChargePointID == chargePointKey);
                if (typeNames != null) query = query.Where(r => typeNames.Contains(r.CertificateType));
                var stale = await query.ToListAsync(cancellationToken);
                _db.InstalledCertificateRecords.RemoveRange(stale);

                var now = DateTime.UtcNow;
                if (response.Status == GetInstalledCertificateStatusEnumType.Accepted)
                {
                    foreach (var chain in response.CertificateHashDataChain ?? new List<CertificateHashDataChainType>())
                    {
                        if (chain?.CertificateHashData == null) continue;
                        _db.InstalledCertificateRecords.Add(new InstalledCertificateRecord
                        {
                            ChargePointID = chargePointKey.Value,
                            CertificateType = chain.CertificateType.ToString(),
                            HashAlgorithm = chain.CertificateHashData.HashAlgorithm.ToString(),
                            IssuerNameHash = Truncate(chain.CertificateHashData.IssuerNameHash, 128),
                            IssuerKeyHash = Truncate(chain.CertificateHashData.IssuerKeyHash, 128),
                            SerialNumber = Truncate(chain.CertificateHashData.SerialNumber, 40),
                            ChildCertificatesJson = chain.ChildCertificateHashData is { Count: > 0 }
                                ? JsonConvert.SerializeObject(chain.ChildCertificateHashData, OCPPMessageFactory.DefaultSettings)
                                : null,
                            ReportedAt = now
                        });
                    }
                }
                await _db.SaveChangesAsync(cancellationToken);
            }
            _logger.LogInformation("GetInstalledCertificateIds answered by {ChargePointId}: {Status}, {Count} certificate(s)",
                chargePointID, response.Status, response.CertificateHashDataChain?.Count ?? 0);
            return response;
        }

        public async Task<TriggerMessageResponse> TriggerCertificateRenewal(string chargePointID, CancellationToken cancellationToken = default)
        {
            EnsureOcpp201(chargePointID);
            // The charger answers, then sends a SignCertificate CSR (handled by SignCertificateHandler).
            var response = await _configurationService.TriggerMessage(chargePointID,
                new TriggerMessageRequest { RequestedMessage = MessageTriggerEnumType.SignChargingStationCertificate, Evse = null! }, cancellationToken);
            _logger.LogInformation("Certificate renewal (TriggerMessage SignChargingStationCertificate) answered by {ChargePointId}: {Status}", chargePointID, response.Status);
            return response;
        }

        public async Task<SecurityProfileChangeResult> SetSecurityProfile(string chargePointID, SetSecurityProfileDto request, CancellationToken cancellationToken = default)
        {
            var profile = request.SecurityProfile;
            if (profile is < 1 or > 3) throw new PkiRequestException("The security profile must be 1, 2 or 3.");
            var chargePoint = await _db.ChargePoints.FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointID, cancellationToken)
                ?? throw new PkiRequestException($"Unknown charge point {chargePointID}.");

            var now = DateTime.UtcNow;
            if (!request.Force)
            {
                if (profile == 3 && string.IsNullOrWhiteSpace(chargePoint.ClientCertThumb) && !await _db.ChargerCertificates.AnyAsync(c =>
                        c.ChargePointID == chargePoint.ID && c.CertificateType == ChargerCertificateType.ChargingStationCertificate
                        && c.Status == ChargerCertificateStatus.Active && c.NotAfter > now, cancellationToken))
                    throw new PkiRequestException("The charger has no active client certificate from the charger CA. Trigger a certificate renewal first (or force the change).");
                if (profile < 3 && string.IsNullOrEmpty(chargePoint.Password))
                    throw new PkiRequestException("Security profiles 1 and 2 use Basic authentication: set a charger password first (or force the change).");
            }

            string chargerStatus;
            string? chargerStatusInfo = null;
            var protocol = _commandSender.GetProtocolVersion(chargePointID);
            if (protocol == null) chargerStatus = "NotConnected";
            else if (protocol != OcppProtocols.Ocpp201) chargerStatus = "NotSent (" + protocol + ")";
            else
            {
                try
                {
                    var response = await _commandSender.SendRequestAsync<SetVariablesRequest, SetVariablesResponse>(chargePointID, "SetVariables",
                        new SetVariablesRequest
                        {
                            SetVariableData = new List<SetVariableDataType>
                            {
                                new()
                                {
                                    AttributeType = AttributeEnumType.Actual,
                                    AttributeValue = profile.ToString(),
                                    Component = new ComponentType { Name = "SecurityCtrlr" },
                                    Variable = new VariableType { Name = "SecurityProfile" }
                                }
                            }
                        }, cancellationToken: cancellationToken);
                    var result = response.setVariableResult?.FirstOrDefault();
                    chargerStatus = result?.attributeStatus?.ToString() ?? "NoResult";
                    chargerStatusInfo = result?.attributeStatusInfo == null ? null : $"{result.attributeStatusInfo.ReasonCode} {result.attributeStatusInfo.AdditionalInfo}".Trim();
                }
                catch (WebSocketNotFoundException) { chargerStatus = "NotConnected"; }
                catch (TimeoutException ex) { chargerStatus = "Timeout"; chargerStatusInfo = ex.Message; }
                catch (OcppCallErrorException ex) { chargerStatus = "CallError"; chargerStatusInfo = $"{ex.ErrorCode} {ex.ErrorDescription}".Trim(); }
            }

            var chargerAccepted = chargerStatus is "Accepted" or "RebootRequired";
            var update = (chargerAccepted || request.Force) && chargePoint.SecurityProfile != profile;
            var previous = chargePoint.SecurityProfile;
            if (update)
            {
                chargePoint.SecurityProfile = profile;
                await _db.SaveChangesAsync(cancellationToken);
            }

            var hints = new List<string>();
            if (chargerStatus == "RebootRequired") hints.Add("Reboot the charger to apply the new security profile.");
            if (!chargerAccepted)
                hints.Add($"SecurityCtrlr.SecurityProfile is read-only on most chargers. Use SetNetworkProfile (ocpp/Configuration/SetNetworkProfile) with connectionData.securityProfile = {profile} and a {(profile == 1 ? "ws:// or wss://" : "wss://")} URL in a free configuration slot, then SetVariables OCPPCommCtrlr.NetworkConfigurationPriority to try that slot first, then Reset.");
            if (profile >= 2) hints.Add("The charger must connect with wss:// and trust the root of the CSMS server certificate (install it as CSMSRootCertificate).");
            if (profile == 3) hints.Add("The charger must present a client certificate issued by the charger CA (CN = charge point id) and no longer uses Basic authentication.");
            if (!update && !chargerAccepted && chargePoint.SecurityProfile != profile)
                hints.Add($"The CSMS still enforces profile {chargePoint.SecurityProfile} for this charger; use force once the charger has switched.");

            _logger.LogInformation("SetSecurityProfile {Profile} for {ChargePointId}: charger {ChargerStatus}, CSMS profile {Previous} -> {Current}",
                profile, chargePointID, chargerStatus, previous, chargePoint.SecurityProfile);
            await _audit.LogAsync("Pki.SetSecurityProfile", "ChargePoint", chargePointID,
                new { Requested = profile, Previous = previous, Current = chargePoint.SecurityProfile, request.Force, ChargerStatus = chargerStatus });
            return new SecurityProfileChangeResult(profile, chargePoint.SecurityProfile, update, chargerStatus, chargerStatusInfo, hints);
        }

        public async Task<ChargePointCertificatesDto?> GetCertificates(string chargePointID, CancellationToken cancellationToken = default)
        {
            var chargePoint = await _db.ChargePoints.AsNoTracking().Where(cp => cp.ChargePointId == chargePointID)
                .Select(cp => new { cp.ID, cp.ChargePointId, cp.SecurityProfile, cp.Password, cp.ClientCertThumb })
                .FirstOrDefaultAsync(cancellationToken);
            if (chargePoint == null) return null;

            var certificates = await _db.ChargerCertificates.AsNoTracking().Where(c => c.ChargePointID == chargePoint.ID)
                .OrderByDescending(c => c.IssuedAt).Take(50).ToListAsync(cancellationToken);
            var installed = await _db.InstalledCertificateRecords.AsNoTracking().Where(r => r.ChargePointID == chargePoint.ID)
                .OrderBy(r => r.CertificateType).ToListAsync(cancellationToken);

            var ca = await _ca.GetCaAsync(cancellationToken);
            var caHashes = ca == null ? new Dictionary<HashAlgorithmEnumType, CertificateHashDataType>()
                : Enum.GetValues<HashAlgorithmEnumType>().ToDictionary(a => a, a => ChargerPkiCrypto.ComputeHashData(ca, ca, a));

            var dtos = certificates.Select(ToDto).ToList();
            var current = certificates.FirstOrDefault(c => c.Status == ChargerCertificateStatus.Active && c.CertificateType == ChargerCertificateType.ChargingStationCertificate);
            var protocol = _commandSender.GetProtocolVersion(chargePointID);
            return new ChargePointCertificatesDto(
                chargePoint.ChargePointId,
                chargePoint.SecurityProfile,
                !string.IsNullOrEmpty(chargePoint.Password),
                !string.IsNullOrWhiteSpace(chargePoint.ClientCertThumb),
                protocol != null,
                protocol,
                ca != null,
                current == null ? null : ToDto(current),
                dtos,
                installed.Select(r => new InstalledCertificateDto(r.ID, r.CertificateType, r.HashAlgorithm, r.IssuerNameHash, r.IssuerKeyHash, r.SerialNumber, r.ReportedAt,
                    Enum.TryParse<HashAlgorithmEnumType>(r.HashAlgorithm, true, out var algorithm) && caHashes.TryGetValue(algorithm, out var caHash)
                    && ChargerPkiCrypto.HashDataMatches(caHash, r.HashAlgorithm, r.IssuerNameHash, r.IssuerKeyHash, r.SerialNumber))).ToList(),
                installed.Count == 0 ? null : installed.Max(r => r.ReportedAt));
        }

        public async Task<ChargerCertificateDto?> RevokeCertificate(string chargePointID, int certificateID, string? reason, CancellationToken cancellationToken = default)
        {
            var certificate = await _db.ChargerCertificates
                .FirstOrDefaultAsync(c => c.ID == certificateID && c.ChargePoint!.ChargePointId == chargePointID, cancellationToken);
            if (certificate == null) return null;
            if (certificate.Status == ChargerCertificateStatus.Revoked) return ToDto(certificate);

            var previous = certificate.Status;
            certificate.Status = ChargerCertificateStatus.Revoked;
            certificate.StatusReason = string.IsNullOrWhiteSpace(reason) ? "Revoked" : Truncate("Revoked: " + reason.Trim(), 512);
            certificate.StatusChangedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("PKI: certificate {Serial} of {ChargePointId} revoked (was {Previous})", certificate.SerialNumber, chargePointID, previous);
            await _audit.LogAsync("Pki.RevokeCertificate", "ChargePoint", chargePointID, new { certificate.SerialNumber, certificate.ThumbprintSha256, reason });
            return ToDto(certificate);
        }

        private void EnsureOcpp201(string chargePointID)
        {
            var protocol = _commandSender.GetProtocolVersion(chargePointID);
            if (protocol != null && protocol != OcppProtocols.Ocpp201)
                throw new PkiRequestException($"Certificate management uses OCPP 2.0.1 messages; {chargePointID} is connected with {protocol}.");
        }

        private Task<int?> ChargePointKey(string chargePointID, CancellationToken cancellationToken) =>
            _db.ChargePoints.AsNoTracking().Where(cp => cp.ChargePointId == chargePointID).Select(cp => (int?)cp.ID).FirstOrDefaultAsync(cancellationToken);

        private static ChargerCertificateDto ToDto(ChargerCertificate c) => new(c.ID, c.CertificateType.ToString(), c.SerialNumber, c.Subject,
            c.ThumbprintSha256, c.NotBefore, c.NotAfter, c.Status.ToString(), c.StatusReason, c.IssuedAt, c.StatusChangedAt);

        private static string Truncate(string? value, int length) => value == null ? "" : value.Length > length ? value[..length] : value;
    }
}
