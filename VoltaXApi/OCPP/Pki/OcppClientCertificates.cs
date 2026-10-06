using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.OCPP.Pki
{
    /// <summary>Where the charger's TLS client certificate comes from: Kestrel's own TLS, or a trusted proxy's header.</summary>
    public static class OcppClientCertificates
    {
        public const string ForwardedItemKey = "Ocpp.ForwardedClientCertificate";

        public static X509Certificate2? Resolve(HttpContext context) =>
            context.Connection.ClientCertificate ?? context.Items[ForwardedItemKey] as X509Certificate2;

        /// <summary>
        /// Runs before UseForwardedHeaders, while RemoteIpAddress is still the TCP peer. The header is always removed
        /// from the request; its certificate is kept (in HttpContext.Items) only when the peer is a trusted proxy.
        /// </summary>
        public static void CaptureForwarded(HttpContext context, OcppTransportSettings settings, ILogger logger)
        {
            if (settings.ClientCertHeader == null || !context.Request.Headers.TryGetValue(settings.ClientCertHeader, out var values))
                return;
            context.Request.Headers.Remove(settings.ClientCertHeader);
            var value = values.ToString();
            if (string.IsNullOrWhiteSpace(value) || !context.Request.Path.StartsWithSegments("/ocpp", StringComparison.OrdinalIgnoreCase))
                return;

            var peer = context.Connection.RemoteIpAddress;
            if (!settings.IsTrustedProxy(peer))
            {
                logger.LogWarning("SECURITY: {Header} sent by {RemoteIp}, which is not in ReverseProxy:KnownProxies; the forwarded client certificate is ignored",
                    settings.ClientCertHeader, peer);
                return;
            }

            var certificate = ChargerPkiCrypto.ParseForwardedCertificate(value);
            if (certificate == null)
            {
                logger.LogWarning("OCPP: the client certificate forwarded in {Header} by {RemoteIp} could not be parsed", settings.ClientCertHeader, peer);
                return;
            }
            context.Items[ForwardedItemKey] = certificate;
        }
    }

    /// <summary>Puts the forwarded-certificate capture in front of the whole pipeline (before UseForwardedHeaders).</summary>
    public sealed class OcppForwardedClientCertificateStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            var services = app.ApplicationServices;
            var settings = OcppTransportSettings.From(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>());
            if (settings.ClientCertHeader != null)
            {
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("VoltaXApi.OCPP.Pki.OcppClientCertificates");
                app.Use(async (context, nextMiddleware) =>
                {
                    OcppClientCertificates.CaptureForwarded(context, settings, logger);
                    await nextMiddleware(context);
                });
            }
            next(app);
        };
    }

    public interface IChargerClientCertificateValidator
    {
        ClientCertificateCheck Validate(X509Certificate2 certificate, string chargePointId);
    }

    /// <summary>Chain to the charger CA + CN == identity + not revoked in ChargerCertificates. Singleton.</summary>
    public sealed class ChargerClientCertificateValidator : IChargerClientCertificateValidator
    {
        private readonly IChargerCertificateAuthority _ca;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChargerClientCertificateValidator> _logger;

        public ChargerClientCertificateValidator(IChargerCertificateAuthority ca, IServiceScopeFactory scopeFactory, ILogger<ChargerClientCertificateValidator> logger)
        {
            _ca = ca;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public ClientCertificateCheck Validate(X509Certificate2 certificate, string chargePointId)
        {
            var ca = _ca.GetCa();
            if (ca == null) return ClientCertificateCheck.NoCa;

            var check = ChargerPkiCrypto.ValidateClientCertificate(certificate, ca, chargePointId, DateTime.UtcNow);
            if (check != ClientCertificateCheck.Valid)
            {
                _logger.LogWarning("OCPP: client certificate {Subject} (serial {Serial}) of {ChargePointId} refused: {Reason}",
                    certificate.Subject, certificate.SerialNumber, chargePointId, check);
                return check;
            }

            var thumbprint = ChargerPkiCrypto.Sha256Thumbprint(certificate);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            // Runs once per handshake; the handshake API (AuthenticationService) is synchronous.
            if (db.ChargerCertificates.AsNoTracking().Any(c => c.ThumbprintSha256 == thumbprint && c.Status == ChargerCertificateStatus.Revoked))
            {
                _logger.LogWarning("OCPP: revoked client certificate {Thumbprint} presented by {ChargePointId}", thumbprint, chargePointId);
                return ClientCertificateCheck.Revoked;
            }
            return ClientCertificateCheck.Valid;
        }
    }
}
