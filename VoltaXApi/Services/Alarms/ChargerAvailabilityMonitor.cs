using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models.Alarms;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.Services.Alarms
{
    /// <summary>
    /// Background check of the chargers' availability (singleton): a charger that came online at least once and has no
    /// connection on any instance for <see cref="AlarmOptions.OfflineAfter"/> raises an Offline alarm, cleared as soon
    /// as it is connected again. A boot loop alarm is cleared once the charger stayed up (no boot) for twice the boot
    /// loop window.
    /// </summary>
    public sealed class ChargerAvailabilityMonitor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ChargePointStatusManagerService _presence;
        private readonly BootHistory _boots;
        private readonly AlarmOptions _options;
        private readonly ILogger<ChargerAvailabilityMonitor> _logger;
        // Last time each charger was seen connected by this monitor; the API start counts as "seen" for all of them, so
        // a restart does not raise an alarm for every charger before they had time to reconnect.
        private readonly ConcurrentDictionary<string, DateTime> _lastSeen = new(StringComparer.Ordinal);
        private readonly DateTime _startedAt;

        public ChargerAvailabilityMonitor(IServiceScopeFactory scopeFactory, ChargePointStatusManagerService presence, BootHistory boots,
            AlarmOptions options, TimeProvider time, ILogger<ChargerAvailabilityMonitor> logger)
        {
            _scopeFactory = scopeFactory;
            _presence = presence;
            _boots = boots;
            _options = options;
            _logger = logger;
            _startedAt = time.GetUtcNow().UtcDateTime;
        }

        /// <summary>Runs one check at <paramref name="nowUtc"/>; returns the number of alarms raised or cleared.</summary>
        public async Task<int> CheckAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            var alarms = scope.ServiceProvider.GetRequiredService<IAlarmService>();

            var chargePoints = await db.ChargePoints.AsNoTracking()
                .Where(cp => db.ChargePointUptimes.Any(u => u.ChargePointID == cp.ID))
                .Select(cp => new
                {
                    cp.ID,
                    cp.ChargePointId,
                    LastOnline = db.ChargePointUptimes.Where(u => u.ChargePointID == cp.ID).Max(u => (DateTime?)u.StartDate),
                    LastStatus = db.ConnectorStatuses.Where(s => db.Connectors.Any(c => c.ID == s.ConnectorID && c.ChargePointID == cp.ID))
                        .Max(s => s.LastStatusTime)
                })
                .ToListAsync(cancellationToken);
            var active = await db.Alarms.AsNoTracking()
                .Where(a => (a.Type == AlarmType.Offline || a.Type == AlarmType.BootLoop) && (a.Status == AlarmStatus.Open || a.Status == AlarmStatus.Acknowledged))
                .Select(a => new { a.ChargePointID, a.Type, a.LastSeenAt })
                .ToListAsync(cancellationToken);

            var changes = 0;
            foreach (var cp in chargePoints)
            {
                try
                {
                    var offlineAlarm = active.Any(a => a.ChargePointID == cp.ID && a.Type == AlarmType.Offline);
                    if (_presence.ChargePointExists(cp.ChargePointId))
                    {
                        _lastSeen[cp.ChargePointId] = nowUtc;
                        if (offlineAlarm) changes += await alarms.ClearAsync(cp.ID, AlarmType.Offline, null, nowUtc, cancellationToken);

                        var bootLoop = active.FirstOrDefault(a => a.ChargePointID == cp.ID && a.Type == AlarmType.BootLoop);
                        var lastBoot = _boots.LastBoot(cp.ChargePointId) ?? bootLoop?.LastSeenAt;
                        if (bootLoop != null && lastBoot < nowUtc - 2 * _options.BootLoopWindow)
                            changes += await alarms.ClearAsync(cp.ID, AlarmType.BootLoop, null, nowUtc, cancellationToken);
                        continue;
                    }
                    if (offlineAlarm) continue;

                    var since = new[] { _startedAt, cp.LastOnline, cp.LastStatus, _lastSeen.TryGetValue(cp.ChargePointId, out var seen) ? seen : null }
                        .Where(d => d.HasValue).Max(d => d!.Value);
                    if (nowUtc - since < _options.OfflineAfter) continue;
                    if (await alarms.RaiseAsync(cp.ID, AlarmRules.Offline(nowUtc - since, since, nowUtc), cancellationToken) != null)
                        changes++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Availability check of {ChargePointId} failed", cp.ChargePointId);
                }
            }
            return changes;
        }
    }

    public sealed class ChargerAvailabilityHostedService : BackgroundService
    {
        private readonly ChargerAvailabilityMonitor _monitor;
        private readonly AlarmOptions _options;
        private readonly ILogger<ChargerAvailabilityHostedService> _logger;

        public ChargerAvailabilityHostedService(ChargerAvailabilityMonitor monitor, AlarmOptions options, ILogger<ChargerAvailabilityHostedService> logger)
        {
            _monitor = monitor;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_options.MonitorInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await _monitor.CheckAsync(DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Charger availability check failed");
                }
            }
        }
    }
}
