using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;

namespace VoltaXApi.OCPP.Pki
{
    /// <summary>
    /// Ocpp:Tls:Enabled = true when Kestrel itself terminates TLS for chargers (no proxy in front). Applies to every
    /// HTTPS endpoint (declare one with ASPNETCORE_URLS=https://+:8443 or Kestrel:Endpoints): server certificate from
    /// Ocpp:Tls:CertificatePath (.pfx with Ocpp:Tls:CertificatePassword, or PEM with Ocpp:Tls:KeyPath), TLS 1.2+,
    /// and a client certificate is requested but optional. The certificate is not judged here: the handshake
    /// (AuthenticationService) checks it against the charger CA for security profile 3 chargers only.
    /// </summary>
    public sealed class OcppKestrelTls : IConfigureOptions<KestrelServerOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OcppKestrelTls> _logger;

        public OcppKestrelTls(IConfiguration configuration, ILogger<OcppKestrelTls> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Configure(KestrelServerOptions options)
        {
            if (!_configuration.GetValue("Ocpp:Tls:Enabled", false)) return;

            var certificatePath = _configuration["Ocpp:Tls:CertificatePath"];
            var serverCertificate = string.IsNullOrWhiteSpace(certificatePath)
                ? null
                : LoadServerCertificate(certificatePath, _configuration["Ocpp:Tls:CertificatePassword"], _configuration["Ocpp:Tls:KeyPath"]);

            options.ConfigureHttpsDefaults(https =>
            {
                if (serverCertificate != null) https.ServerCertificate = serverCertificate;
                https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                // Charger certificates chain to our own CA, not to the OS trust store: validated per charger at the handshake.
                https.ClientCertificateValidation = (_, _, _) => true;
            });
            _logger.LogInformation("OCPP TLS: Kestrel HTTPS endpoints request client certificates{Certificate}",
                serverCertificate == null ? " (server certificate from the Kestrel configuration)" : $" (server certificate {serverCertificate.Subject})");
        }

        private static X509Certificate2 LoadServerCertificate(string path, string? password, string? keyPath)
        {
            if (path.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
                return X509CertificateLoader.LoadPkcs12FromFile(path, password);
            if (string.IsNullOrWhiteSpace(keyPath))
                throw new InvalidOperationException("Ocpp:Tls:KeyPath is required with a PEM Ocpp:Tls:CertificatePath.");
            using var pem = string.IsNullOrEmpty(password)
                ? X509Certificate2.CreateFromPemFile(path, keyPath)
                : X509Certificate2.CreateFromEncryptedPemFile(path, password, keyPath);
            // SslStream on Windows cannot use the ephemeral key of a PEM-loaded certificate.
            return X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), null);
        }
    }
}
