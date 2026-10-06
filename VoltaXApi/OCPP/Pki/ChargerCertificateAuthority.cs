using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.OCPP.Pki
{
    public sealed record ChargerCaInfo(bool Configured, string Source, string? Subject, string? SerialNumber,
        string? ThumbprintSha256, DateTime? NotBefore, DateTime? NotAfter, string? KeyAlgorithm, string? Error);

    public interface IChargerCertificateAuthority
    {
        /// <summary>The CA certificate with its private key, or null when no CA is configured.</summary>
        Task<X509Certificate2?> GetCaAsync(CancellationToken cancellationToken = default);
        /// <summary>Synchronous variant for the WebSocket handshake; the CA is normally loaded at startup.</summary>
        X509Certificate2? GetCa();
        Task<ChargerCaInfo> DescribeAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The CA that signs charger client certificates (security profile 3). Loaded once from
    /// Pki:CaCertificatePem/CaPrivateKeyPem, else Pki:CaCertificatePath/CaKeyPath, else (Pki:AutoCreateDevCa,
    /// default only in Development) a self-signed ECDSA P-256 CA kept in the database with its key encrypted
    /// by Data Protection. Singleton.
    /// </summary>
    public sealed class ChargerCertificateAuthority : IChargerCertificateAuthority
    {
        public const string DevelopmentCaName = "development";
        private const string ProtectorPurpose = "VoltaX.Pki.CaPrivateKey.v1";

        private readonly PkiSettings _settings;
        private readonly bool _autoCreateDevCa;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDataProtectionProvider _dataProtection;
        private readonly ILogger<ChargerCertificateAuthority> _logger;
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private volatile bool _loaded;
        private X509Certificate2? _ca;
        private string _source = "None";
        private string? _error;

        public ChargerCertificateAuthority(IConfiguration configuration, IHostEnvironment environment, IServiceScopeFactory scopeFactory,
            IDataProtectionProvider dataProtection, ILogger<ChargerCertificateAuthority> logger)
        {
            _settings = PkiSettings.From(configuration);
            _autoCreateDevCa = _settings.AutoCreateDevCa ?? environment.IsDevelopment();
            _scopeFactory = scopeFactory;
            _dataProtection = dataProtection;
            _logger = logger;
        }

        public X509Certificate2? GetCa() => _loaded ? _ca : GetCaAsync().GetAwaiter().GetResult();

        public async Task<X509Certificate2?> GetCaAsync(CancellationToken cancellationToken = default)
        {
            if (_loaded) return _ca;
            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                if (_loaded) return _ca;
                try
                {
                    _ca = await LoadAsync(cancellationToken);
                    _error = null;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Not cached: a database that is not migrated yet or a bad file is retried on the next call.
                    _error = ex.Message;
                    _logger.LogError(ex, "PKI: the charger CA could not be loaded ({Source}); certificate signing and security profile 3 are unavailable", _source);
                    return null;
                }
                _loaded = true;
                if (_ca == null)
                    _logger.LogWarning("PKI: no charger CA configured (Pki:CaCertificatePath/CaKeyPath, Pki:CaCertificatePem/CaPrivateKeyPem or Pki:AutoCreateDevCa); SignCertificate requests are rejected");
                else
                {
                    _logger.LogInformation("PKI: charger CA {Subject} loaded from {Source}, valid until {NotAfter:o}", _ca.Subject, _source, _ca.NotAfter.ToUniversalTime());
                    if (_ca.NotAfter.ToUniversalTime() < DateTime.UtcNow.AddDays(90))
                        _logger.LogWarning("PKI: the charger CA expires on {NotAfter:o}", _ca.NotAfter.ToUniversalTime());
                }
                return _ca;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        public async Task<ChargerCaInfo> DescribeAsync(CancellationToken cancellationToken = default)
        {
            var ca = await GetCaAsync(cancellationToken);
            if (ca == null) return new ChargerCaInfo(false, _source, null, null, null, null, null, null, _error);
            return new ChargerCaInfo(true, _source, ca.Subject, ca.SerialNumber, ChargerPkiCrypto.Sha256Thumbprint(ca),
                ca.NotBefore.ToUniversalTime(), ca.NotAfter.ToUniversalTime(),
                ca.GetECDsaPublicKey() != null ? "ECDSA" : ca.GetRSAPublicKey() != null ? "RSA" : ca.PublicKey.Oid.FriendlyName, null);
        }

        private async Task<X509Certificate2?> LoadAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_settings.CaCertificatePem) || !string.IsNullOrWhiteSpace(_settings.CaPrivateKeyPem))
            {
                _source = "Configuration (Pki:CaCertificatePem)";
                if (string.IsNullOrWhiteSpace(_settings.CaCertificatePem) || string.IsNullOrWhiteSpace(_settings.CaPrivateKeyPem))
                    throw new InvalidOperationException("Pki:CaCertificatePem and Pki:CaPrivateKeyPem must both be set.");
                return Validate(FromPem(DecodePemSetting(_settings.CaCertificatePem), DecodePemSetting(_settings.CaPrivateKeyPem)));
            }

            if (!string.IsNullOrWhiteSpace(_settings.CaCertificatePath))
            {
                _source = "File (Pki:CaCertificatePath)";
                var path = _settings.CaCertificatePath;
                if (path.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
                    return Validate(X509CertificateLoader.LoadPkcs12FromFile(path, _settings.CaKeyPassword, X509KeyStorageFlags.EphemeralKeySet));
                if (string.IsNullOrWhiteSpace(_settings.CaKeyPath))
                    throw new InvalidOperationException("Pki:CaKeyPath is required with a PEM Pki:CaCertificatePath.");
                return Validate(FromPem(await File.ReadAllTextAsync(path, cancellationToken), await File.ReadAllTextAsync(_settings.CaKeyPath, cancellationToken)));
            }

            if (!_autoCreateDevCa)
            {
                _source = "None";
                return null;
            }

            _source = "Development CA (database)";
            return await LoadOrCreateDevelopmentCaAsync(cancellationToken);
        }

        private X509Certificate2 FromPem(string certificatePem, string keyPem)
        {
            if (keyPem.Contains("ENCRYPTED PRIVATE KEY", StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(_settings.CaKeyPassword))
                    throw new InvalidOperationException("The CA private key is encrypted: set Pki:CaKeyPassword.");
                return X509Certificate2.CreateFromEncryptedPem(certificatePem, keyPem, _settings.CaKeyPassword);
            }
            return X509Certificate2.CreateFromPem(certificatePem, keyPem);
        }

        /// <summary>Accepts the PEM text itself or base64 of it (handy in environment variables).</summary>
        public static string DecodePemSetting(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.Contains("-----BEGIN", StringComparison.Ordinal)) return trimmed.Replace("\\n", "\n");
            return Encoding.UTF8.GetString(Convert.FromBase64String(trimmed));
        }

        private X509Certificate2 Validate(X509Certificate2 ca)
        {
            if (!ca.HasPrivateKey) throw new InvalidOperationException("The charger CA certificate has no private key.");
            var constraints = ca.Extensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();
            if (constraints == null || !constraints.CertificateAuthority)
                throw new InvalidOperationException("The charger CA certificate is not a CA (basicConstraints cA=true is required).");
            if (ca.NotAfter.ToUniversalTime() < DateTime.UtcNow)
                _logger.LogError("PKI: the charger CA {Subject} expired on {NotAfter:o}", ca.Subject, ca.NotAfter.ToUniversalTime());
            return ca;
        }

        private async Task<X509Certificate2> LoadOrCreateDevelopmentCaAsync(CancellationToken cancellationToken)
        {
            var protector = _dataProtection.CreateProtector(ProtectorPurpose);
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();

            var stored = await db.PkiCertificateAuthorities.AsNoTracking().FirstOrDefaultAsync(a => a.Name == DevelopmentCaName, cancellationToken);
            if (stored != null)
                return Validate(X509Certificate2.CreateFromPem(stored.CertificatePem, protector.Unprotect(stored.PrivateKeyProtected)));

            var now = DateTimeOffset.UtcNow;
            using var created = ChargerPkiCrypto.CreateDevelopmentCa("CN=EVEON Charger Development CA, O=EVEON", now);
            using var key = created.GetECDsaPrivateKey()!;
            var certificatePem = ChargerPkiCrypto.ToPem(created);
            var keyPem = PemEncoding.WriteString("PRIVATE KEY", key.ExportPkcs8PrivateKey());
            db.PkiCertificateAuthorities.Add(new PkiCertificateAuthority
            {
                Name = DevelopmentCaName,
                CertificatePem = certificatePem,
                PrivateKeyProtected = protector.Protect(keyPem),
                NotAfter = created.NotAfter.ToUniversalTime(),
                CreatedAt = now.UtcDateTime
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("PKI: created a self-signed DEVELOPMENT charger CA {Subject}. Do not use it in production.", created.Subject);
                return X509Certificate2.CreateFromPem(certificatePem, keyPem);
            }
            catch (DbUpdateException ex)
            {
                // Another instance created it first (unique Name): use theirs.
                _logger.LogInformation(ex, "PKI: development CA created concurrently; reloading");
                await using var retryScope = _scopeFactory.CreateAsyncScope();
                var retryDb = retryScope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                var winner = await retryDb.PkiCertificateAuthorities.AsNoTracking().FirstAsync(a => a.Name == DevelopmentCaName, cancellationToken);
                return Validate(X509Certificate2.CreateFromPem(winner.CertificatePem, protector.Unprotect(winner.PrivateKeyProtected)));
            }
        }
    }
}
