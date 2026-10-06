using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models.Ocpp201;

namespace VoltaXApi.Controllers
{
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

    /// <summary>Charger events (NotifyEvent) and monitors (NotifyMonitoringReport). Read access: ViewChargePoints.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ChargerEventController : ControllerBase
    {
        private readonly VoltaXApiDbContext _db;

        public ChargerEventController(VoltaXApiDbContext db) => _db = db;

        /// <summary>Events, most recent first. <paramref name="alarmsOnly"/> keeps active events of severity &lt;= 3 or of fault-like variables.</summary>
        [HttpGet]
        public async Task<IActionResult> GetEvents(CancellationToken cancellationToken, string? chargePointId = null, DateTime? from = null, DateTime? to = null,
            int? maxSeverity = null, bool alarmsOnly = false, string? component = null, string? variable = null, string? search = null,
            int page = 1, int pageSize = 25)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _db.ChargerEvents.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(chargePointId)) query = query.Where(e => e.ChargePointID == chargePointId);
            if (from.HasValue) { var f = from.Value.ToUniversalTime(); query = query.Where(e => e.Timestamp >= f); }
            if (to.HasValue) { var t = to.Value.ToUniversalTime(); query = query.Where(e => e.Timestamp <= t); }
            if (maxSeverity.HasValue) query = query.Where(e => e.Severity != null && e.Severity <= maxSeverity);
            if (alarmsOnly) query = query.Where(AlarmFilter);
            if (!string.IsNullOrWhiteSpace(component)) query = query.Where(e => e.ComponentName == component);
            if (!string.IsNullOrWhiteSpace(variable)) query = query.Where(e => e.VariableName == variable);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.ChargePointID.Contains(search) || e.TechCode!.Contains(search) || e.TechInfo!.Contains(search)
                                         || e.ActualValue!.Contains(search) || e.ComponentName.Contains(search) || e.VariableName.Contains(search));

            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(e => e.Timestamp).ThenByDescending(e => e.ID)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return Ok(new PagedResult<ChargerEvent>(items, total, page, pageSize));
        }

        [HttpGet("monitors/{chargePointID}")]
        public async Task<IActionResult> GetMonitors(string chargePointID, CancellationToken cancellationToken) =>
            Ok(await _db.VariableMonitors.AsNoTracking().Where(m => m.ChargePointID == chargePointID)
                .OrderBy(m => m.ComponentName).ThenBy(m => m.VariableName).ThenBy(m => m.MonitoringId)
                .Take(1000).ToListAsync(cancellationToken));

        // SQL form of ChargerEventMapper.IsAlarm.
        private static readonly Expression<Func<ChargerEvent, bool>> AlarmFilter = e =>
            e.Cleared != true && (e.Severity <= 3 ||
            (e.VariableName.Contains("Problem") || e.VariableName.Contains("Tripped") || e.VariableName.Contains("Fault") ||
             e.VariableName.Contains("Overheat") || e.VariableName.Contains("Fallback") || e.VariableName.Contains("Overcurrent") ||
             e.VariableName.Contains("Overvoltage") || e.VariableName.Contains("Undervoltage"))
            && (e.ActualValue == null || e.ActualValue != "false" && e.ActualValue != "0"));
    }

    /// <summary>Display messages reported by chargers (NotifyDisplayMessages). Read access: ViewChargePoints.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ChargerDisplayMessageController : ControllerBase
    {
        private readonly VoltaXApiDbContext _db;

        public ChargerDisplayMessageController(VoltaXApiDbContext db) => _db = db;

        /// <summary>The latest snapshot (most recent GetDisplayMessages answer) of the charger.</summary>
        [HttpGet("{chargePointID}")]
        public async Task<IActionResult> GetLatestDisplayMessages(string chargePointID, CancellationToken cancellationToken)
        {
            var latest = await _db.DisplayMessageSnapshots.AsNoTracking()
                .Where(m => m.ChargePointID == chargePointID)
                .OrderByDescending(m => m.ReceivedAt).ThenByDescending(m => m.ID)
                .Select(m => new { m.RequestId, m.ReceivedAt })
                .FirstOrDefaultAsync(cancellationToken);
            if (latest == null) return Ok(new { requestId = (int?)null, receivedAt = (DateTime?)null, messages = Array.Empty<DisplayMessageSnapshot>() });
            var messages = await _db.DisplayMessageSnapshots.AsNoTracking()
                .Where(m => m.ChargePointID == chargePointID && m.RequestId == latest.RequestId && m.MessageId != null)
                .OrderBy(m => m.MessageId)
                .ToListAsync(cancellationToken);
            return Ok(new { requestId = (int?)latest.RequestId, receivedAt = (DateTime?)latest.ReceivedAt, messages });
        }
    }

    /// <summary>Customer data returned by chargers (CustomerInformation / NotifyCustomerInformation). Read access: ViewUsers (personal data).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerInformationReportController : ControllerBase
    {
        private readonly VoltaXApiDbContext _db;

        public CustomerInformationReportController(VoltaXApiDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetCustomerInformationReports(CancellationToken cancellationToken, string? chargePointId = null, int page = 1, int pageSize = 25)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _db.CustomerInformationReports.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(chargePointId)) query = query.Where(r => r.ChargePointID == chargePointId);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.ID)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new CustomerInformationReportRow(r.ID, r.ChargePointID, r.RequestId, r.Report, r.Clear, r.CustomerIdentifier, r.IdToken,
                    r.CommandStatus, r.RequestedAt, r.PartsReceived, r.Complete, r.CompletedAt, r.Data))
                .ToListAsync(cancellationToken);
            return Ok(new PagedResult<CustomerInformationReportRow>(items, total, page, pageSize));
        }

        public sealed record CustomerInformationReportRow(int ID, string ChargePointID, int RequestId, bool Report, bool Clear, string? CustomerIdentifier,
            string? IdToken, string? CommandStatus, DateTime RequestedAt, int PartsReceived, bool Complete, DateTime? CompletedAt, string? Data);
    }
}
