using VoltaXApi.ScaleOut;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;

namespace VoltaXApi.Ocpi.Services
{
    // Pushes changes to registered eMSPs: EVSE status (PATCH), locations (PUT), and delivers the
    // persisted outbox (sessions, CDRs, command results) with exponential backoff.
    public class OcpiPushWorker : BackgroundService
    {
        public const int MaxAttempts = 10;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
        private const string EvseStatusCursor = "evse-status";
        private const string LocationsCursor = "locations";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OcpiOptions _options;
        private readonly ILogger<OcpiPushWorker> _logger;
        private readonly IClusterJobLease _lease;

        public OcpiPushWorker(IServiceScopeFactory scopeFactory, IOptions<OcpiOptions> options, ILogger<OcpiPushWorker> logger, IClusterJobLease lease)
        {
            _lease = lease;
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        public static TimeSpan Backoff(int attempts) =>
            TimeSpan.FromSeconds(Math.Min(3600, 30 * Math.Pow(2, Math.Max(0, attempts - 1))));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled) return;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only one replica runs this job (see ScaleOut/ClusterJobLease.cs).
                    if (!await _lease.TryAcquireAsync("ocpi-push", TimeSpan.FromMinutes(1), stoppingToken))
                    {
                        await Task.Delay(Interval, stoppingToken);
                        continue;
                    }
                    using var scope = _scopeFactory.CreateScope();
                    var services = scope.ServiceProvider;
                    await ScanEvseStatusesAsync(services, stoppingToken);
                    await ScanLocationsAsync(services, stoppingToken);
                    await ExpirePendingSessionsAsync(services, stoppingToken);
                    await DeliverAsync(services, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Missing tables (migration not applied) or a database outage: retry on the next tick.
                    _logger.LogError(ex, "OCPI push cycle failed");
                }
                await Task.Delay(Interval, stoppingToken);
            }
        }

        private static async Task<OcpiSyncCursor> CursorAsync(VoltaXApiDbContext db, string name, CancellationToken cancellationToken)
        {
            var cursor = await db.OcpiSyncCursors.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
            if (cursor != null) return cursor;
            // First start: partners pull the initial state, only later changes are pushed.
            cursor = new OcpiSyncCursor { Name = name, Position = DateTime.UtcNow };
            db.OcpiSyncCursors.Add(cursor);
            await db.SaveChangesAsync(cancellationToken);
            return cursor;
        }

        private async Task ScanEvseStatusesAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            var db = services.GetRequiredService<VoltaXApiDbContext>();
            var parties = services.GetRequiredService<OcpiPartyService>();
            var data = services.GetRequiredService<OcpiDataService>();
            var cursor = await CursorAsync(db, EvseStatusCursor, cancellationToken);
            var changes = await db.ConnectorStatuses.IgnoreQueryFilters().AsNoTracking()
                .Where(s => s.UpdatedAt > cursor.Position && s.Connector != null && s.Connector.ChargePointID != null)
                .OrderBy(s => s.UpdatedAt).Take(1000)
                .Select(s => new { s.UpdatedAt, ChargePointID = s.Connector!.ChargePointID!.Value, s.Connector.EvseID })
                .ToListAsync(cancellationToken);
            if (changes.Count == 0) return;

            var receivers = await parties.ReceiversAsync("locations", cancellationToken);
            foreach (var evse in changes.Select(c => (c.ChargePointID, c.EvseID)).Distinct())
            {
                if (receivers.Count == 0) break;
                var current = await data.EvseAsync(evse.ChargePointID, evse.EvseID, cancellationToken);
                // Removals are pushed with the full location by the location scan.
                if (current == null || current.Value.Evse.Status == "REMOVED") continue;
                var patch = new EvseStatusPatchDto { Status = current.Value.Evse.Status, LastUpdated = current.Value.Evse.LastUpdated };
                foreach (var (party, url) in receivers)
                    await parties.EnqueueAsync(party.ID, "locations", HttpMethod.Patch,
                        $"{url}/{_options.CountryCode}/{_options.PartyId}/{current.Value.LocationId}/{current.Value.Evse.Uid}",
                        patch, coalesce: true, cancellationToken: cancellationToken);
            }
            cursor.Position = changes.Max(c => c.UpdatedAt);
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task ScanLocationsAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            var db = services.GetRequiredService<VoltaXApiDbContext>();
            var parties = services.GetRequiredService<OcpiPartyService>();
            var data = services.GetRequiredService<OcpiDataService>();
            var cursor = await CursorAsync(db, LocationsCursor, cancellationToken);
            var since = cursor.Position;
            var stations = await db.ChargingStations.IgnoreQueryFilters().AsNoTracking()
                .Where(s => s.UpdatedAt > since).Select(s => new { s.ID, s.UpdatedAt }).ToListAsync(cancellationToken);
            var points = await db.ChargePoints.IgnoreQueryFilters().AsNoTracking()
                .Where(cp => cp.UpdatedAt > since).Select(cp => new { ID = cp.ChargingStationID, cp.UpdatedAt }).ToListAsync(cancellationToken);
            var connectors = await db.Connectors.IgnoreQueryFilters().AsNoTracking()
                .Where(c => c.UpdatedAt > since && c.ChargePoint != null).Select(c => new { ID = c.ChargePoint!.ChargingStationID, c.UpdatedAt }).ToListAsync(cancellationToken);
            var changed = stations.Concat(points).Concat(connectors).ToList();
            if (changed.Count == 0) return;

            var receivers = await parties.ReceiversAsync("locations", cancellationToken);
            foreach (var stationId in changed.Select(c => c.ID).Distinct())
            {
                if (receivers.Count == 0) break;
                var station = await db.ChargingStations.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.ID == stationId, cancellationToken);
                // Private stations are never announced; deleted ones are pushed with REMOVED EVSEs.
                if (station == null || !station.IsDeleted && !OcpiMapper.IsPublished(station)) continue;
                var location = await data.LocationAsync(OcpiMapper.LocationId(stationId), cancellationToken);
                if (location == null) continue;
                foreach (var (party, url) in receivers)
                    await parties.EnqueueAsync(party.ID, "locations", HttpMethod.Put,
                        $"{url}/{_options.CountryCode}/{_options.PartyId}/{location.Id}", location, coalesce: true, cancellationToken: cancellationToken);
            }
            cursor.Position = changed.Max(c => c.UpdatedAt);
            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task ExpirePendingSessionsAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            var db = services.GetRequiredService<VoltaXApiDbContext>();
            var limit = DateTime.UtcNow.AddMinutes(-15);
            await db.OcpiSessions.Where(s => s.Status == OcpiSessionStatus.Pending && s.CreatedAt < limit)
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.Status, OcpiSessionStatus.Invalid).SetProperty(s => s.LastUpdated, DateTime.UtcNow), cancellationToken);
            var purge = DateTime.UtcNow.AddDays(-7);
            await db.OcpiOutbox.Where(m => m.Status == OcpiOutboxStatus.Sent && m.SentAt < purge).ExecuteDeleteAsync(cancellationToken);
        }

        private async Task DeliverAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            var db = services.GetRequiredService<VoltaXApiDbContext>();
            var parties = services.GetRequiredService<OcpiPartyService>();
            var client = services.GetRequiredService<OcpiClient>();
            var now = DateTime.UtcNow;
            var due = await db.OcpiOutbox.Include(m => m.OcpiParty)
                .Where(m => m.Status == OcpiOutboxStatus.Pending && m.NextAttemptAt <= now)
                .OrderBy(m => m.ID).Take(50).ToListAsync(cancellationToken);
            foreach (var message in due)
            {
                var party = message.OcpiParty!;
                if (party.Status == OcpiPartyStatus.Suspended)
                {
                    message.NextAttemptAt = now.AddMinutes(10);
                    continue;
                }
                var token = party.Status == OcpiPartyStatus.Registered ? parties.OutgoingToken(party) : null;
                if (token == null)
                {
                    message.Status = OcpiOutboxStatus.Failed;
                    message.LastError = "Partner is not registered";
                    continue;
                }
                try
                {
                    await client.SendAsync<object>(new HttpMethod(message.Method), message.Url, token, message.PayloadJson,
                        party.CountryCode, party.PartyId, cancellationToken);
                    message.Status = OcpiOutboxStatus.Sent;
                    message.SentAt = DateTime.UtcNow;
                    message.LastError = null;
                }
                catch (Exception ex) when (ex is OcpiClientException or HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
                {
                    message.Attempts++;
                    message.LastError = ex.Message.Length <= 1024 ? ex.Message : ex.Message[..1024];
                    var permanent = ex is OcpiClientException { Permanent: true };
                    if (permanent || message.Attempts >= MaxAttempts)
                    {
                        message.Status = OcpiOutboxStatus.Failed;
                        _logger.LogWarning("OCPI {Module} {Method} to party {PartyID} failed for good: {Error}", message.Module, message.Method, party.ID, message.LastError);
                    }
                    else
                        message.NextAttemptAt = DateTime.UtcNow + Backoff(message.Attempts);
                }
            }
            if (due.Count > 0) await db.SaveChangesAsync(cancellationToken);
        }
    }
}
