using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Hubs;
using VoltaXApi.Models.Ocpp201;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>Persists OCPP 2.0.1 device data: events, monitors, customer information and display messages. Scoped.</summary>
    public interface IOcppDeviceDataService
    {
        Task<List<ChargerEvent>> RecordEventsAsync(string chargePointId, NotifyEventRequest request, CancellationToken cancellationToken = default);
        Task ApplyMonitoringReportAsync(string chargePointId, int requestId, IReadOnlyCollection<MonitoringDataType> monitors, bool replaceAll, CancellationToken cancellationToken = default);
        Task ApplySetMonitoringResultAsync(string chargePointId, SetVariableMonitoringRequest request, SetVariableMonitoringResponse response, CancellationToken cancellationToken = default);
        Task RemoveMonitorsAsync(string chargePointId, IEnumerable<int> monitoringIds, CancellationToken cancellationToken = default);
        Task RecordCustomerInformationRequestAsync(string chargePointId, CustomerInformationRequest request, string? commandStatus, CancellationToken cancellationToken = default);
        Task<CustomerInformationReport> AppendCustomerInformationAsync(string chargePointId, NotifyCustomerInformationRequest request, CancellationToken cancellationToken = default);
        Task RecordDisplayMessagesAsync(string chargePointId, int requestId, IReadOnlyCollection<MessageInfoType> messages, CancellationToken cancellationToken = default);
    }

    public class OcppDeviceDataService : IOcppDeviceDataService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly ILogger<OcppDeviceDataService> _logger;

        public OcppDeviceDataService(VoltaXApiDbContext db, ILogger<OcppDeviceDataService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<ChargerEvent>> RecordEventsAsync(string chargePointId, NotifyEventRequest request, CancellationToken cancellationToken = default)
        {
            var monitorIds = (request.EventData ?? new List<EventDataType>())
                .Where(e => e?.VariableMonitoringId != null).Select(e => e.VariableMonitoringId!.Value).Distinct().ToList();
            var severities = monitorIds.Count == 0
                ? new Dictionary<int, int>()
                : await _db.VariableMonitors.AsNoTracking()
                    .Where(m => m.ChargePointID == chargePointId && monitorIds.Contains(m.MonitoringId))
                    .GroupBy(m => m.MonitoringId)
                    .Select(g => new { g.Key, Severity = g.Min(m => m.Severity) })
                    .ToDictionaryAsync(x => x.Key, x => x.Severity, cancellationToken);

            var events = ChargerEventMapper.Map(chargePointId, request, DateTime.UtcNow, severities);
            _db.ChargerEvents.AddRange(events);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("NotifyEvent => {Count} event(s) stored for {ChargePointId} (seqNo {SeqNo}, tbc {Tbc})",
                events.Count, chargePointId, request.SeqNo, request.Tbc);
            return events;
        }

        public async Task ApplyMonitoringReportAsync(string chargePointId, int requestId, IReadOnlyCollection<MonitoringDataType> monitors, bool replaceAll, CancellationToken cancellationToken = default)
        {
            var rows = ChargerEventMapper.MapMonitors(chargePointId, requestId, monitors, DateTime.UtcNow);
            var ids = rows.Select(r => r.MonitoringId).ToList();
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            var stale = replaceAll
                ? _db.VariableMonitors.Where(m => m.ChargePointID == chargePointId)
                : _db.VariableMonitors.Where(m => m.ChargePointID == chargePointId && ids.Contains(m.MonitoringId));
            var removed = await stale.ExecuteDeleteAsync(cancellationToken);
            _db.VariableMonitors.AddRange(rows);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            _logger.LogInformation("NotifyMonitoringReport => {ChargePointId} request {RequestId}: {Count} monitor(s) stored, {Removed} replaced (full report: {Full})",
                chargePointId, requestId, rows.Count, removed, replaceAll);
        }

        public async Task ApplySetMonitoringResultAsync(string chargePointId, SetVariableMonitoringRequest request, SetVariableMonitoringResponse response, CancellationToken cancellationToken = default)
        {
            var requested = request.SetMonitoringData ?? new List<SetMonitoringDataType>();
            var results = response.SetMonitoringResult ?? new List<SetMonitoringResultType>();
            var monitors = new List<MonitoringDataType>();
            // Results come back in request order; the value is only in the request.
            for (var i = 0; i < results.Count && i < requested.Count; i++)
            {
                var result = results[i];
                if (result == null || result.Status != SetMonitoringStatusEnumType.Accepted || result.Id == null) continue;
                monitors.Add(new MonitoringDataType
                {
                    Component = result.Component ?? requested[i].Component,
                    Variable = result.Variable ?? requested[i].Variable,
                    VariableMonitoring = new List<VariableMonitoringType>
                    {
                        new() { Id = result.Id.Value, Type = result.Type, Severity = result.Severity, Value = requested[i].Value, Transaction = requested[i].Transaction }
                    }
                });
            }
            if (monitors.Count > 0)
                await ApplyMonitoringReportAsync(chargePointId, 0, monitors, replaceAll: false, cancellationToken);
        }

        public async Task RemoveMonitorsAsync(string chargePointId, IEnumerable<int> monitoringIds, CancellationToken cancellationToken = default)
        {
            var ids = monitoringIds.Distinct().ToList();
            if (ids.Count == 0) return;
            var removed = await _db.VariableMonitors.Where(m => m.ChargePointID == chargePointId && ids.Contains(m.MonitoringId)).ExecuteDeleteAsync(cancellationToken);
            _logger.LogInformation("ClearVariableMonitoring => {Removed} monitor(s) removed for {ChargePointId}", removed, chargePointId);
        }

        public async Task RecordCustomerInformationRequestAsync(string chargePointId, CustomerInformationRequest request, string? commandStatus, CancellationToken cancellationToken = default)
        {
            var report = await _db.CustomerInformationReports
                .Where(r => r.ChargePointID == chargePointId && r.RequestId == request.RequestId)
                .OrderByDescending(r => r.ID).FirstOrDefaultAsync(cancellationToken);
            // The charger may already have sent its data before this command returned; keep that row.
            if (report == null || report.CommandStatus != null)
            {
                report = new CustomerInformationReport { ChargePointID = chargePointId, RequestId = request.RequestId, RequestedAt = DateTime.UtcNow };
                _db.CustomerInformationReports.Add(report);
            }
            report.Report = request.Report;
            report.Clear = request.Clear;
            report.CustomerIdentifier = ChargerEventMapper.Truncate(request.CustomerIdentifier, 64);
            report.IdToken = ChargerEventMapper.Truncate(request.IdToken?.IdToken, 36);
            report.CommandStatus = commandStatus;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<CustomerInformationReport> AppendCustomerInformationAsync(string chargePointId, NotifyCustomerInformationRequest request, CancellationToken cancellationToken = default)
        {
            var report = await _db.CustomerInformationReports
                .Where(r => r.ChargePointID == chargePointId && r.RequestId == request.RequestId && !r.Complete)
                .OrderByDescending(r => r.ID).FirstOrDefaultAsync(cancellationToken);
            if (report == null)
            {
                report = new CustomerInformationReport { ChargePointID = chargePointId, RequestId = request.RequestId, RequestedAt = DateTime.UtcNow };
                _db.CustomerInformationReports.Add(report);
            }
            var result = CustomerInformationAssembler.Append(report.PartsJson, request.SeqNo, request.Data, request.Tbc);
            report.PartsReceived = result.PartsReceived;
            report.Data = result.Data;
            report.LastPartAt = DateTime.UtcNow;
            if (result.Complete)
            {
                report.Complete = true;
                report.CompletedAt = DateTime.UtcNow;
                report.PartsJson = null;
            }
            else report.PartsJson = result.PartsJson;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("NotifyCustomerInformation => {ChargePointId} request {RequestId} part {SeqNo} stored (complete: {Complete})",
                chargePointId, request.RequestId, request.SeqNo, report.Complete);
            return report;
        }

        public async Task RecordDisplayMessagesAsync(string chargePointId, int requestId, IReadOnlyCollection<MessageInfoType> messages, CancellationToken cancellationToken = default)
        {
            var rows = ChargerEventMapper.MapDisplayMessages(chargePointId, requestId, messages, DateTime.UtcNow);
            if (rows.Count == 0)
            {
                if (await _db.DisplayMessageSnapshots.AnyAsync(m => m.ChargePointID == chargePointId && m.RequestId == requestId, cancellationToken)) return;
                rows.Add(new DisplayMessageSnapshot { ChargePointID = chargePointId, RequestId = requestId, ReceivedAt = DateTime.UtcNow });
            }
            else
                // A charger that first answered "no message" for this request (marker row) now reports some.
                await _db.DisplayMessageSnapshots.Where(m => m.ChargePointID == chargePointId && m.RequestId == requestId && m.MessageId == null)
                    .ExecuteDeleteAsync(cancellationToken);
            _db.DisplayMessageSnapshots.AddRange(rows);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("NotifyDisplayMessages => {Count} message(s) stored for {ChargePointId} request {RequestId}",
                messages.Count, chargePointId, requestId);
        }
    }

    /// <summary>Pushes alarm-worthy charger events to the dashboard over the charger hub. Singleton.</summary>
    public sealed class ChargerAlarmNotifier
    {
        /// <summary>Hub group of the global alarm feed (joined with ChargerHub.JoinChargerGroup, requires ViewChargePoints).</summary>
        public const string AlarmGroup = "#charger-events";
        public const string HubMethod = "ChargerEvent";

        private readonly IHubContext<ChargerHub> _hub;
        private readonly ILogger<ChargerAlarmNotifier> _logger;

        public ChargerAlarmNotifier(IHubContext<ChargerHub> hub, ILogger<ChargerAlarmNotifier> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public async Task NotifyAsync(IEnumerable<ChargerEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (var e in events.Where(ChargerEventMapper.IsAlarm))
            {
                var payload = new
                {
                    e.ID, e.ChargePointID, e.EventId, e.Timestamp, e.Trigger, e.ActualValue, e.TechCode, e.TechInfo, e.Cleared,
                    e.TransactionId, e.ComponentName, e.VariableName, e.EvseId, e.ConnectorId, e.Severity, e.EventNotificationType
                };
                try
                {
                    await _hub.Clients.Groups(e.ChargePointID, AlarmGroup).SendAsync(HubMethod, payload, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Could not push charger event {EventId} of {ChargePointId} to the dashboard", e.EventId, e.ChargePointID);
                }
            }
        }
    }
}
