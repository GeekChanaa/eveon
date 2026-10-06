using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Ocpp16;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.SmartCharging
{
    public sealed record SessionAllocationView(
        int TransactionID,
        string? TransactionUid,
        int ChargePointID,
        string ChargePointIdentity,
        int EvseId,
        int? ConnectorId,
        DateTime StartedAt,
        double? EvMaxCurrentA,
        double AllocatedA,
        double AllocatedKW,
        bool Queued,
        string SendStatus,
        string? Reason,
        DateTime? LastSentAt);

    public sealed record RebalanceSummary(
        int ChargingStationID,
        bool Enabled,
        double StationLimitA,
        double AllocatedA,
        int Sent,
        int Unchanged,
        int Failed,
        IReadOnlyList<SessionAllocationView> Sessions);

    public interface ILoadBalancingService
    {
        /// <summary>Allocates the station limit between its running sessions and sends the changed TxProfiles. Callers serialize per station (see <see cref="LoadBalancingTrigger"/>).</summary>
        Task<RebalanceSummary> RebalanceStationAsync(int chargingStationId, string reason, CancellationToken cancellationToken = default);

        /// <summary>Running sessions of the station with their last allocation, without sending anything.</summary>
        Task<RebalanceSummary> GetLiveAllocationsAsync(int chargingStationId, CancellationToken cancellationToken = default);
    }

    public class LoadBalancingService : ILoadBalancingService
    {
        /// <summary>An increase of at most this much is not resent (decreases always are, so the station limit holds).</summary>
        public const double ResendThresholdA = 1.0;

        private sealed record ActiveSession(
            int TransactionID, string? Uid, DateTime StartTime, int ChargePointDbId, string ChargePointIdentity, int EvseId, int? ConnectorId);

        private readonly VoltaXApiDbContext _db;
        private readonly ISmartChargingService _smartCharging;
        private readonly ILogger<LoadBalancingService> _logger;

        public LoadBalancingService(VoltaXApiDbContext db, ISmartChargingService smartCharging, ILogger<LoadBalancingService> logger)
        {
            _db = db;
            _smartCharging = smartCharging;
            _logger = logger;
        }

        public async Task<RebalanceSummary> RebalanceStationAsync(int chargingStationId, string reason, CancellationToken cancellationToken = default)
        {
            var limit = await _db.StationLoadLimits.SingleOrDefaultAsync(l => l.ChargingStationID == chargingStationId, cancellationToken);
            var sessions = await LoadActiveSessions(chargingStationId, cancellationToken);
            var chargePointIds = await _db.ChargePoints.Where(c => c.ChargingStationID == chargingStationId).Select(c => c.ID).ToListAsync(cancellationToken);
            var lbProfiles = await _db.ChargingProfiles
                .Where(p => chargePointIds.Contains(p.ChargePointID) && p.Source == ChargingProfileSourceEnum.LoadBalancer
                    && p.Status != ChargingProfileStatusEnum.Cleared)
                .ToListAsync(cancellationToken);

            // A TxProfile ends with its transaction on the charger.
            var activeUids = sessions.Where(s => s.Uid != null).Select(s => s.Uid!).ToHashSet();
            foreach (var ended in lbProfiles.Where(p => p.TransactionId == null || !activeUids.Contains(p.TransactionId)))
            {
                ended.Status = ChargingProfileStatusEnum.Cleared;
                ended.LastError = "Transaction ended";
            }
            await _db.SaveChangesAsync(cancellationToken);

            if (limit is not { Enabled: true })
            {
                await ReleaseSessions(lbProfiles.Where(p => p.Status != ChargingProfileStatusEnum.Cleared).ToList(), cancellationToken);
                return new RebalanceSummary(chargingStationId, false, 0, 0, 0, 0, 0, Array.Empty<SessionAllocationView>());
            }

            var settings = LoadLimitSettings.From(limit);
            var evMax = await LoadEvMaxCurrents(sessions, cancellationToken);
            var allocation = StationLoadAllocator.Allocate(settings,
                sessions.Select(s => new SessionDemand(s.TransactionID, s.StartTime, evMax.GetValueOrDefault(s.TransactionID))).ToList());
            var byTransaction = sessions.ToDictionary(s => s.TransactionID);

            var txDefaultStack = await _db.ChargingProfiles
                .Where(p => chargePointIds.Contains(p.ChargePointID) && p.Purpose == ChargingProfilePurposeEnum.TxDefaultProfile
                    && (p.Status == ChargingProfileStatusEnum.Accepted || p.Status == ChargingProfileStatusEnum.Pending))
                .GroupBy(p => p.ChargePointID)
                .Select(g => new { ChargePointID = g.Key, Max = g.Max(p => p.StackLevel) })
                .ToDictionaryAsync(g => g.ChargePointID, g => g.Max, cancellationToken);

            // Decreases first, so the sum of the limits on the chargers never exceeds the station limit in between.
            var plan = allocation.Sessions
                .Select(a =>
                {
                    var session = byTransaction[a.TransactionId];
                    var profile = lbProfiles.FirstOrDefault(p => p.Status != ChargingProfileStatusEnum.Cleared && p.TransactionId == session.Uid);
                    var current = profile == null ? (double?)null : SmartChargingJson.Deserialize<ChargingProfilePeriod>(profile.PeriodsJson).FirstOrDefault()?.Limit;
                    return (Allocation: a, Session: session, Profile: profile, CurrentA: current);
                })
                .OrderBy(x => x.Allocation.AllocatedA - (x.CurrentA ?? double.MaxValue / 4))
                .ToList();

            int sent = 0, unchanged = 0, failed = 0;
            foreach (var (alloc, session, profile, currentA) in plan)
            {
                if (profile is { Status: ChargingProfileStatusEnum.Accepted } && currentA is { } current
                    && alloc.AllocatedA >= current && alloc.AllocatedA - current <= ResendThresholdA)
                {
                    unchanged++;
                    continue;
                }
                if (session.Uid == null)
                {
                    _logger.LogWarning("Load balancing: transaction {TransactionID} on station {StationId} has no OCPP id; not limited", session.TransactionID, chargingStationId);
                    failed++;
                    continue;
                }

                var record = new StationLoadAllocation
                {
                    ChargingStationID = chargingStationId,
                    TransactionID = session.TransactionID,
                    ChargePointID = session.ChargePointDbId,
                    EvseId = session.EvseId,
                    ConnectorId = session.ConnectorId,
                    AllocatedA = alloc.AllocatedA,
                    AllocatedKW = StationLoadAllocator.ToKW(alloc.AllocatedA, settings),
                    StationLimitA = allocation.StationLimitA,
                    Queued = alloc.Queued,
                    Reason = Truncate(reason, 512)
                };
                try
                {
                    var input = new ChargingProfileInputDto
                    {
                        ChargingProfileID = profile?.ID,
                        EvseId = session.EvseId,
                        StackLevel = txDefaultStack.TryGetValue(session.ChargePointDbId, out var stack) ? stack + 1 : 0,
                        Purpose = ChargingProfilePurposeEnum.TxProfile,
                        Kind = ChargingProfileKindEnum.Relative,
                        ChargingRateUnit = ChargingRateUnitEnum.A,
                        TransactionId = session.Uid,
                        Periods = new List<ChargingProfilePeriod> { new(0, alloc.AllocatedA, settings.Phases is >= 1 and <= 3 ? settings.Phases : null) }
                    };
                    var result = await _smartCharging.SendChargingProfileAsync(session.ChargePointIdentity, input,
                        ChargingProfileSourceEnum.LoadBalancer, null, null, cancellationToken);
                    record.ChargingProfileID = result.Profile.ID;
                    record.SendStatus = result.Status;
                    if (result.Reason != null) record.Reason = Truncate($"{reason}; {result.Reason}", 512);
                    if (result.Status == "Accepted") sent++; else failed++;
                }
                catch (Exception ex) when (ex is TimeoutException or WebSocketNotFoundException or OcppCallErrorException or ValidationException or NotFoundException)
                {
                    record.SendStatus = ex switch
                    {
                        TimeoutException => "Timeout",
                        WebSocketNotFoundException => "NotConnected",
                        OcppCallErrorException => "CallError",
                        _ => "Invalid"
                    };
                    record.Reason = Truncate($"{reason}; {ex.Message}", 512);
                    record.ChargingProfileID = profile?.ID;
                    failed++;
                    _logger.LogWarning(ex, "Load balancing: TxProfile for transaction {TransactionID} on {ChargePointId} not applied ({Status})",
                        session.TransactionID, session.ChargePointIdentity, record.SendStatus);
                }
                _db.StationLoadAllocations.Add(record);
                await _db.SaveChangesAsync(cancellationToken);
            }

            limit.LastRebalancedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Load balancing station {StationId} ({Reason}): limit {LimitA} A, {Sessions} session(s), {Allocated} A allocated, {Sent} sent, {Unchanged} unchanged, {Failed} failed",
                chargingStationId, reason, allocation.StationLimitA, sessions.Count, allocation.TotalA, sent, unchanged, failed);

            var live = await GetLiveAllocationsAsync(chargingStationId, cancellationToken);
            return live with { Sent = sent, Unchanged = unchanged, Failed = failed };
        }

        public async Task<RebalanceSummary> GetLiveAllocationsAsync(int chargingStationId, CancellationToken cancellationToken = default)
        {
            var limit = await _db.StationLoadLimits.AsNoTracking().SingleOrDefaultAsync(l => l.ChargingStationID == chargingStationId, cancellationToken);
            var sessions = await LoadActiveSessions(chargingStationId, cancellationToken);
            var transactionIds = sessions.Select(s => s.TransactionID).ToList();
            var latest = await _db.StationLoadAllocations.AsNoTracking()
                .Where(a => transactionIds.Contains(a.TransactionID))
                .GroupBy(a => a.TransactionID)
                .Select(g => g.OrderByDescending(a => a.ID).First())
                .ToListAsync(cancellationToken);
            var evMax = await LoadEvMaxCurrents(sessions, cancellationToken);
            var settings = limit == null ? null : LoadLimitSettings.From(limit);

            var views = sessions.Select(s =>
            {
                var last = latest.FirstOrDefault(a => a.TransactionID == s.TransactionID);
                return new SessionAllocationView(s.TransactionID, s.Uid, s.ChargePointDbId, s.ChargePointIdentity, s.EvseId, s.ConnectorId,
                    s.StartTime, evMax.GetValueOrDefault(s.TransactionID), last?.AllocatedA ?? 0, last?.AllocatedKW ?? 0, last?.Queued ?? false,
                    last?.SendStatus ?? "NotAllocated", last?.Reason, last?.CreatedAt);
            }).ToList();
            return new RebalanceSummary(chargingStationId, limit?.Enabled == true,
                settings == null ? 0 : StationLoadAllocator.EffectiveLimitA(settings),
                views.Sum(v => v.AllocatedA), 0, 0, 0, views);
        }

        /// <summary>Load balancing switched off: the sessions get their limits removed.</summary>
        private async Task ReleaseSessions(List<ChargingProfile> profiles, CancellationToken cancellationToken)
        {
            foreach (var profile in profiles)
            {
                var identity = await _db.ChargePoints.Where(c => c.ID == profile.ChargePointID).Select(c => c.ChargePointId).FirstOrDefaultAsync(cancellationToken);
                if (identity == null)
                {
                    profile.Status = ChargingProfileStatusEnum.Cleared;
                    profile.LastError = "Charge point deleted";
                    await _db.SaveChangesAsync(cancellationToken);
                    continue;
                }
                try
                {
                    await _smartCharging.ClearStoredChargingProfileAsync(identity, profile.ID, cancellationToken);
                }
                catch (Exception ex) when (ex is TimeoutException or WebSocketNotFoundException or OcppCallErrorException)
                {
                    _logger.LogWarning(ex, "Load balancing disabled: TxProfile {OcppProfileId} on {ChargePointId} not cleared; retried on the next pass",
                        profile.OcppProfileId, identity);
                }
            }
        }

        private async Task<List<ActiveSession>> LoadActiveSessions(int chargingStationId, CancellationToken cancellationToken)
        {
            var rows = await _db.Transactions.AsNoTracking()
                .Where(t => t.Status == TransactionStatusEnum.Current && t.StopTime == null
                    && t.Connector != null && t.Connector.ChargePoint != null && t.Connector.ChargePoint.ChargingStationID == chargingStationId)
                .Select(t => new
                {
                    t.ID,
                    t.Uid,
                    t.StartTime,
                    ChargePointDbId = t.Connector!.ChargePoint!.ID,
                    Identity = t.Connector.ChargePoint.ChargePointId,
                    t.Connector.EvseID,
                    t.Connector.ConnectorID
                })
                .ToListAsync(cancellationToken);

            // A 1.6 TxProfile targets the 1.6 connector the transaction runs on.
            var ocpp16Ids = rows.Select(r => Ocpp16Transaction.TryParseUid(r.Uid, out var id) && r.Uid!.StartsWith(Ocpp16Transaction.UidPrefix) ? id : 0)
                .Where(id => id > 0).ToList();
            var ocpp16Connectors = ocpp16Ids.Count == 0
                ? new Dictionary<int, int>()
                : await _db.Ocpp16Transactions.AsNoTracking().Where(t => ocpp16Ids.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id, t => t.ConnectorId, cancellationToken);

            return rows.Select(r =>
            {
                var evseId = r.EvseID;
                if (r.Uid != null && r.Uid.StartsWith(Ocpp16Transaction.UidPrefix) && Ocpp16Transaction.TryParseUid(r.Uid, out var id16)
                    && ocpp16Connectors.TryGetValue(id16, out var connector16))
                    evseId = connector16;
                return new ActiveSession(r.ID, r.Uid, DateTime.SpecifyKind(r.StartTime, DateTimeKind.Utc), r.ChargePointDbId, r.Identity, evseId, r.ConnectorID);
            }).ToList();
        }

        /// <summary>EV maximum current (AC, from NotifyEVChargingNeeds received during the session) per transaction.</summary>
        private async Task<Dictionary<int, double?>> LoadEvMaxCurrents(List<ActiveSession> sessions, CancellationToken cancellationToken)
        {
            if (sessions.Count == 0) return new Dictionary<int, double?>();
            var chargePointIds = sessions.Select(s => s.ChargePointDbId).Distinct().ToList();
            var since = sessions.Min(s => s.StartTime);
            var needs = await _db.EvChargingNeeds.AsNoTracking()
                .Where(n => chargePointIds.Contains(n.ChargePointID) && n.ReceivedAt >= since && n.EvMaxCurrent != null)
                .OrderByDescending(n => n.ReceivedAt)
                .ToListAsync(cancellationToken);
            return sessions.ToDictionary(s => s.TransactionID, s => needs
                .FirstOrDefault(n => n.ChargePointID == s.ChargePointDbId && n.EvseId == s.EvseId && n.ReceivedAt >= s.StartTime
                    && (n.RequestedEnergyTransfer == null || n.RequestedEnergyTransfer.StartsWith("AC")))?.EvMaxCurrent);
        }

        private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
    }
}
