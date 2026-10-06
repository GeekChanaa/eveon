using VoltaXApi.ScaleOut;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Pki
{
    /// <summary>
    /// Loads the charger CA at startup, then once a day asks every connected charger whose active client
    /// certificate expires within Pki:RenewBeforeDays (30) to renew it (TriggerMessage SignChargingStationCertificate),
    /// and notifies the dashboard about expiring charger certificates and an expiring CA.
    /// </summary>
    public sealed class ChargerCertificateExpiryMonitor : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IChargerCertificateAuthority _ca;
        private readonly IOcppCommandSender _commandSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChargerCertificateExpiryMonitor> _logger;

        private readonly IClusterJobLease _lease;

        public ChargerCertificateExpiryMonitor(IServiceScopeFactory scopeFactory, IChargerCertificateAuthority ca, IOcppCommandSender commandSender,
            IConfiguration configuration, ILogger<ChargerCertificateExpiryMonitor> logger, IClusterJobLease lease)
        {
            _lease = lease;
            _scopeFactory = scopeFactory;
            _ca = ca;
            _commandSender = commandSender;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Profile 3 handshakes need the CA; load it before the first charger connects. Errors are logged by the CA.
            await _ca.GetCaAsync(stoppingToken);

            try { await Task.Delay(StartupDelay, stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only one replica runs this job (see ScaleOut/ClusterJobLease.cs).
                    if (await _lease.TryAcquireAsync("pki-expiry-monitor", Interval + TimeSpan.FromHours(1), stoppingToken))
                        await CheckAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "PKI: certificate expiry check failed");
                }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { return; }
            }
        }

        public async Task CheckAsync(CancellationToken cancellationToken)
        {
            var renewBeforeDays = Math.Max(1, PkiSettings.From(_configuration).RenewBeforeDays);
            var now = DateTime.UtcNow;
            var threshold = now.AddDays(renewBeforeDays);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            var expiring = await db.ChargerCertificates.AsNoTracking()
                .Where(c => c.Status == ChargerCertificateStatus.Active && c.CertificateType == ChargerCertificateType.ChargingStationCertificate && c.NotAfter <= threshold)
                .Join(db.ChargePoints, c => c.ChargePointID, cp => cp.ID, (c, cp) => new { cp.ChargePointId, c.SerialNumber, c.NotAfter })
                .ToListAsync(cancellationToken);

            var requested = 0;
            foreach (var certificate in expiring)
            {
                var protocol = _commandSender.GetProtocolVersion(certificate.ChargePointId);
                if (protocol != OcppProtocols.Ocpp201)
                {
                    _logger.LogWarning("PKI: certificate {Serial} of {ChargePointId} expires {NotAfter:o}; charger {State}, renewal not requested",
                        certificate.SerialNumber, certificate.ChargePointId, certificate.NotAfter, protocol == null ? "offline" : "speaks " + protocol);
                    continue;
                }
                try
                {
                    var security = scope.ServiceProvider.GetRequiredService<ISecurityService>();
                    var response = await security.TriggerCertificateRenewal(certificate.ChargePointId, cancellationToken);
                    _logger.LogInformation("PKI: renewal of certificate {Serial} (expires {NotAfter:o}) requested from {ChargePointId}: {Status}",
                        certificate.SerialNumber, certificate.NotAfter, certificate.ChargePointId, response.Status);
                    if (response.Status == Messages.TriggerMessageStatusEnumType.Accepted) requested++;
                }
                catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException or WebSocketNotFoundException)
                {
                    _logger.LogWarning(ex, "PKI: renewal request to {ChargePointId} failed", certificate.ChargePointId);
                }
            }

            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            if (expiring.Count > 0)
            {
                var expired = expiring.Count(c => c.NotAfter <= now);
                await notifications.NotifyDashboardAsync(new DashboardNotification("Charge Point", "ChargerCertificateExpiring",
                        $"{expiring.Count} charger client certificate(s) expire within {renewBeforeDays} days ({expired} already expired); renewal accepted by {requested} charger(s).",
                        Urgent: expired > 0),
                    "EditChargePoints", "/dashboard/charging-points");
            }

            var ca = await _ca.GetCaAsync(cancellationToken);
            if (ca != null && ca.NotAfter.ToUniversalTime() <= now.AddDays(Math.Max(90, renewBeforeDays * 3)))
            {
                _logger.LogWarning("PKI: the charger CA {Subject} expires on {NotAfter:o}", ca.Subject, ca.NotAfter.ToUniversalTime());
                await notifications.NotifyDashboardAsync(new DashboardNotification("Charge Point", "ChargerCaExpiring",
                        $"The charger CA expires on {ca.NotAfter.ToUniversalTime():yyyy-MM-dd}. Plan a CA rollover before charger certificates stop validating.", Urgent: true),
                    "EditChargePoints", "/dashboard/pki");
            }
        }
    }
}
