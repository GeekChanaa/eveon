using System.Net;

namespace VoltaXApi.OCPP.Pki
{
    /// <summary>Configuration section "Pki" (charger CA).</summary>
    public sealed class PkiSettings
    {
        /// <summary>PEM certificate file of the charger CA, or a PKCS#12 (.pfx/.p12) file holding certificate and key.</summary>
        public string? CaCertificatePath { get; set; }
        /// <summary>PEM private key file (PKCS#8, optionally encrypted with CaKeyPassword).</summary>
        public string? CaKeyPath { get; set; }
        /// <summary>CA certificate as PEM or base64 of the PEM (environment variable friendly).</summary>
        public string? CaCertificatePem { get; set; }
        /// <summary>CA private key as PEM or base64 of the PEM.</summary>
        public string? CaPrivateKeyPem { get; set; }
        public string? CaKeyPassword { get; set; }
        /// <summary>Create (once) and use a self-signed development CA stored encrypted in the database. Default: true only in Development.</summary>
        public bool? AutoCreateDevCa { get; set; }
        public int ChargerCertificateDays { get; set; } = 365;
        public int RenewBeforeDays { get; set; } = 30;

        public static PkiSettings From(IConfiguration configuration) =>
            configuration.GetSection("Pki").Get<PkiSettings>() ?? new PkiSettings();
    }

    /// <summary>OCPP transport security settings ("Ocpp" / "Ocpp:Tls" / "ReverseProxy:KnownProxies").</summary>
    public sealed class OcppTransportSettings
    {
        /// <summary>Security profile 1 over plain ws:// (no TLS). Default: true only in Development.</summary>
        public bool AllowInsecureProfile1 { get; init; }
        /// <summary>Header in which the TLS-terminating proxy forwards the client certificate (e.g. X-Client-Cert). Null: not read.</summary>
        public string? ClientCertHeader { get; init; }
        /// <summary>Proxies whose ClientCertHeader is trusted (ReverseProxy:KnownProxies, plus loopback).</summary>
        public IReadOnlyCollection<IPAddress> TrustedProxies { get; init; } = Array.Empty<IPAddress>();

        public static OcppTransportSettings From(IConfiguration configuration, IHostEnvironment environment)
        {
            var proxies = new List<IPAddress> { IPAddress.Loopback, IPAddress.IPv6Loopback };
            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
                if (IPAddress.TryParse(proxy, out var address)) proxies.Add(address);
            var header = configuration["Ocpp:Tls:ClientCertHeader"];
            return new OcppTransportSettings
            {
                AllowInsecureProfile1 = configuration.GetValue("Ocpp:AllowInsecureProfile1", environment.IsDevelopment()),
                ClientCertHeader = string.IsNullOrWhiteSpace(header) ? null : header.Trim(),
                TrustedProxies = proxies
            };
        }

        public bool IsTrustedProxy(IPAddress? address)
        {
            if (address == null) return false;
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            return TrustedProxies.Any(p => p.Equals(address));
        }
    }
}
