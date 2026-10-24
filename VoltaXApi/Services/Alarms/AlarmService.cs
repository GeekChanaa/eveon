using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models.Alarms;
using VoltaXApi.Models.Ocpp201;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services.Alarms
{
    /// <summary>
    /// Raises, counts and clears charge point alarms from what the chargers report, and applies the operator's
    /// actions (acknowledge, assign, resolve). Scoped. Charger hooks never throw: an alarm failure must not fail the
    /// OCPP message that triggered it.
    /// </summary>
    public interface IAlarmService
    {
        Task OnStatus16Async(string chargePointId, int connectorId, string status, string? errorCode, string? info, string? vendorErrorCode, DateTime at);
        Task OnStatus201Async(string chargePointId, int evseId, int connectorId, ConnectorStatusEnumType status, DateTime at);
        Task OnEventsAsync(string chargePointId, IReadOnlyCollection<ChargerEvent> events);
        /// <summary>An accepted BootNotification: raises a boot loop alarm when the charger reboots too often.</summary>
        Task OnAcceptedBootAsync(string chargePointId, DateTime at);

        Task<Alarm?> RaiseAsync(int chargePointId, AlarmSignal signal, CancellationToken cancellationToken = default);
        /// <summary>Clears (closes automatically) the active alarms of a charge point that match.</summary>
        Task<int> ClearAsync(int chargePointId, AlarmType type, Func<Alarm, bool>? match, DateTime at, CancellationToken cancellationToken = default);

        Task<AlarmActionResult> AcknowledgeAsync(long id, int userId, CancellationToken cancellationToken = default);
        Task<AlarmActionResult> AssignAsync(long id, int? assigneeId, int userId, CancellationToken cancellationToken = default);
        Task<AlarmActionResult> ResolveAsync(long id, int userId, string comment, CancellationToken cancellationToken = default);
    }

    public enum AlarmActionOutcome { Done, NotFound, Closed, InvalidAssignee }

    public sealed record AlarmActionResult(AlarmActionOutcome Outcome, Alarm? Alarm = null);

    public class AlarmService : IAlarmService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly AlarmNotifier _notifier;
        private readonly BootHistory _boots;
        private readonly AlarmOptions _options;
        private readonly TimeProvider _time;
        private readonly ILogger<AlarmService> _logger;

        public AlarmService(VoltaXApiDbContext db, AlarmNotifier notifier, BootHistory boots, AlarmOptions options, TimeProvider time, ILogger<AlarmService> logger)
        {
            _db = db;
            _notifier = notifier;
            _boots = boots;
            _options = options;
            _time = time;
            _logger = logger;
        }

        private DateTime Now => _time.GetUtcNow().UtcDateTime;

        public Task OnStatus16Async(string chargePointId, int connectorId, string status, string? errorCode, string? info, string? vendorErrorCode, DateTime at) =>
            Safely(chargePointId, "StatusNotification", async cp =>
            {
                var signal = AlarmRules.FromStatus16(connectorId, status, errorCode, info, vendorErrorCode, at);
                int? evse = connectorId > 0 ? connectorId : null;
                if (signal != null) await RaiseAsync(cp, signal);
                // A notification without error means every fault of this connector is gone.
                else await ClearAsync(cp, AlarmType.ConnectorFault, a => a.EvseId == evse, at);
            });

        public Task OnStatus201Async(string chargePointId, int evseId, int connectorId, ConnectorStatusEnumType status, DateTime at) =>
            Safely(chargePointId, "StatusNotification", async cp =>
            {
                var signal = AlarmRules.FromStatus201(evseId, connectorId, status, at);
                int? evse = evseId > 0 ? evseId : null;
                int? connector = evseId > 0 ? connectorId : null;
                if (signal != null) await RaiseAsync(cp, signal);
                else await ClearAsync(cp, AlarmType.ConnectorFault, a => a.EvseId == evse && a.ConnectorNumber == connector, at);
            });

        public Task OnEventsAsync(string chargePointId, IReadOnlyCollection<ChargerEvent> events) =>
            events.Count == 0 ? Task.CompletedTask : Safely(chargePointId, "NotifyEvent", async cp =>
            {
                foreach (var e in events.OrderBy(e => e.Timestamp))
                {
                    var signal = AlarmRules.FromEvent(e);
                    if (signal != null) { await RaiseAsync(cp, signal); continue; }
                    if (!AlarmRules.ClearsEvent(e)) continue;
                    var code = AlarmRules.EventCode(e);
                    int? evse = e.EvseId;
                    int? connector = e.EvseId.HasValue ? e.ConnectorId : null;
                    await ClearAsync(cp, AlarmType.DeviceEvent, a => a.Code == code && a.EvseId == evse && a.ConnectorNumber == connector, e.Timestamp);
                }
            });

        public Task OnAcceptedBootAsync(string chargePointId, DateTime at) =>
            Safely(chargePointId, "BootNotification", async cp =>
            {
                var boots = _boots.Record(chargePointId, at, _options.BootLoopWindow);
                if (boots >= _options.BootLoopCount)
                    await RaiseAsync(cp, AlarmRules.BootLoop(boots, _options.BootLoopWindow, at));
            });

        public Task<Alarm?> RaiseAsync(int chargePointId, AlarmSignal signal, CancellationToken cancellationToken = default) =>
            RaiseAsync(chargePointId, signal, retry: true, cancellationToken);

        private async Task<Alarm?> RaiseAsync(int chargePointId, AlarmSignal signal, bool retry, CancellationToken cancellationToken)
        {
            var chargePoint = await _db.ChargePoints.AsNoTracking().Where(c => c.ID == chargePointId)
                .Select(c => new { c.ID, c.ChargePointId }).FirstOrDefaultAsync(cancellationToken);
            if (chargePoint == null) return null;

            var now = Now;
            var at = signal.At > now ? now : signal.At;
            var key = Alarm.KeyOf(signal.Type, chargePointId, signal.EvseId, signal.ConnectorNumber, signal.Code);
            var alarm = await _db.Alarms.FirstOrDefaultAsync(a => a.OpenKey == key, cancellationToken);
            var created = alarm == null;
            var previousSeverity = alarm?.Severity;
            if (alarm == null)
            {
                alarm = new Alarm
                {
                    Type = signal.Type,
                    Code = signal.Code,
                    Status = AlarmStatus.Open,
                    ChargePointID = chargePointId,
                    ChargePointIdentity = chargePoint.ChargePointId,
                    EvseId = signal.EvseId,
                    ConnectorNumber = signal.ConnectorNumber,
                    ConnectorID = await ConnectorRowAsync(chargePointId, signal, cancellationToken),
                    FirstSeenAt = at,
                    LastSeenAt = at,
                    Count = 1,
                    Severity = signal.Severity,
                    DedupKey = key,
                    OpenKey = key,
                    CreatedAt = now
                };
                _db.Alarms.Add(alarm);
            }
            else
            {
                alarm.Count++;
                if (at > alarm.LastSeenAt) alarm.LastSeenAt = at;
                if (signal.Severity > alarm.Severity) alarm.Severity = signal.Severity;
            }
            alarm.Description = signal.Description;
            alarm.Details = signal.Details ?? alarm.Details;
            alarm.UpdatedAt = now;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (created && retry)
            {
                // Raised meanwhile by another instance or the availability monitor (unique OpenKey): count on that one.
                _db.Entry(alarm).State = EntityState.Detached;
                return await RaiseAsync(chargePointId, signal, retry: false, cancellationToken);
            }

            if (created)
                _logger.LogWarning("Alarm {AlarmId} raised: {Type} {Code} ({Severity}) on {ChargePointId} EVSE {EvseId}",
                    alarm.ID, alarm.Type, alarm.Code, alarm.Severity, alarm.ChargePointIdentity, alarm.EvseId);
            var dto = await AlarmQueries.FindAsync(_db, alarm.ID, cancellationToken);
            await _notifier.PublishAsync(created ? AlarmChange.Raised : AlarmChange.Updated, dto);
            if (dto != null && alarm.Severity == AlarmSeverity.Critical && (created || previousSeverity != AlarmSeverity.Critical))
                _ = _notifier.NotifyCritical(dto);
            return alarm;
        }

        public async Task<int> ClearAsync(int chargePointId, AlarmType type, Func<Alarm, bool>? match, DateTime at, CancellationToken cancellationToken = default)
        {
            var active = await _db.Alarms
                .Where(a => a.ChargePointID == chargePointId && a.Type == type && (a.Status == AlarmStatus.Open || a.Status == AlarmStatus.Acknowledged))
                .ToListAsync(cancellationToken);
            var cleared = active.Where(a => match == null || match(a)).ToList();
            if (cleared.Count == 0) return 0;
            var now = Now;
            foreach (var alarm in cleared)
            {
                alarm.ClearedAt = at > now || at < alarm.FirstSeenAt ? now : at;
                alarm.Close(AlarmStatus.Cleared, now);
            }
            await _db.SaveChangesAsync(cancellationToken);
            foreach (var alarm in cleared)
            {
                _logger.LogInformation("Alarm {AlarmId} cleared: {Type} {Code} on {ChargePointId}", alarm.ID, alarm.Type, alarm.Code, alarm.ChargePointIdentity);
                await _notifier.PublishAsync(AlarmChange.Cleared, await AlarmQueries.FindAsync(_db, alarm.ID, cancellationToken));
            }
            return cleared.Count;
        }

        public async Task<AlarmActionResult> AcknowledgeAsync(long id, int userId, CancellationToken cancellationToken = default)
        {
            var alarm = await _db.Alarms.FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
            if (alarm == null) return new(AlarmActionOutcome.NotFound);
            if (!alarm.IsActive) return new(AlarmActionOutcome.Closed, alarm);
            if (alarm.Status == AlarmStatus.Open)
            {
                alarm.Status = AlarmStatus.Acknowledged;
                alarm.AcknowledgedAt = Now;
                alarm.AcknowledgedByID = userId;
                alarm.UpdatedAt = Now;
                await _db.SaveChangesAsync(cancellationToken);
                await _notifier.PublishAsync(AlarmChange.Acknowledged, await AlarmQueries.FindAsync(_db, alarm.ID, cancellationToken));
            }
            return new(AlarmActionOutcome.Done, alarm);
        }

        public async Task<AlarmActionResult> AssignAsync(long id, int? assigneeId, int userId, CancellationToken cancellationToken = default)
        {
            var alarm = await _db.Alarms.FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
            if (alarm == null) return new(AlarmActionOutcome.NotFound);
            if (!alarm.IsActive) return new(AlarmActionOutcome.Closed, alarm);
            if (assigneeId != null && !await AlarmQueries.Assignees(_db).AnyAsync(u => u.ID == assigneeId, cancellationToken))
                return new(AlarmActionOutcome.InvalidAssignee, alarm);

            alarm.AssignedToID = assigneeId;
            alarm.AssignedAt = assigneeId == null ? null : Now;
            // Taking an alarm (or giving it to someone) acknowledges it.
            if (assigneeId != null && alarm.Status == AlarmStatus.Open)
            {
                alarm.Status = AlarmStatus.Acknowledged;
                alarm.AcknowledgedAt = Now;
                alarm.AcknowledgedByID = userId;
            }
            alarm.UpdatedAt = Now;
            await _db.SaveChangesAsync(cancellationToken);
            await _notifier.PublishAsync(AlarmChange.Assigned, await AlarmQueries.FindAsync(_db, alarm.ID, cancellationToken));
            return new(AlarmActionOutcome.Done, alarm);
        }

        public async Task<AlarmActionResult> ResolveAsync(long id, int userId, string comment, CancellationToken cancellationToken = default)
        {
            var alarm = await _db.Alarms.FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
            if (alarm == null) return new(AlarmActionOutcome.NotFound);
            if (!alarm.IsActive) return new(AlarmActionOutcome.Closed, alarm);
            var now = Now;
            alarm.ResolvedAt = now;
            alarm.ResolvedByID = userId;
            alarm.ResolutionComment = comment.Trim();
            if (alarm.AcknowledgedAt == null)
            {
                alarm.AcknowledgedAt = now;
                alarm.AcknowledgedByID = userId;
            }
            alarm.Close(AlarmStatus.Resolved, now);
            await _db.SaveChangesAsync(cancellationToken);
            await _notifier.PublishAsync(AlarmChange.Resolved, await AlarmQueries.FindAsync(_db, alarm.ID, cancellationToken));
            return new(AlarmActionOutcome.Done, alarm);
        }

        private async Task<int?> ConnectorRowAsync(int chargePointId, AlarmSignal signal, CancellationToken cancellationToken)
        {
            if (signal.EvseId == null) return null;
            var connectors = _db.Connectors.AsNoTracking().Where(c => c.ChargePointID == chargePointId && c.EvseID == signal.EvseId);
            if (signal.ConnectorNumber != null) connectors = connectors.Where(c => c.ConnectorID == signal.ConnectorNumber || c.ConnectorID == null);
            return await connectors.OrderBy(c => c.ConnectorID).Select(c => (int?)c.ID).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>Runs a charger hook for a known charge point; logs instead of throwing.</summary>
        private async Task Safely(string chargePointId, string message, Func<int, Task> work)
        {
            try
            {
                var id = await _db.ChargePoints.AsNoTracking().Where(c => c.ChargePointId == chargePointId).Select(c => (int?)c.ID).FirstOrDefaultAsync();
                if (id == null) return;
                await work(id.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alarms => {Message} of {ChargePointId} could not be processed", message, chargePointId);
            }
        }
    }
}
