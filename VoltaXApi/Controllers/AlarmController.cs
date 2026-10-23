using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Helpers;
using VoltaXApi.Models.Alarms;
using VoltaXApi.Services;
using VoltaXApi.Services.Alarms;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Controllers
{
    public sealed class AssignAlarmDto
    {
        /// <summary>The user who takes the alarm; null gives it back (unassigned).</summary>
        public int? AssignedToID { get; set; }
    }

    public sealed class ResolveAlarmDto
    {
        [Required, StringLength(1000, MinimumLength = 3)]
        public string Comment { get; set; } = "";
    }

    /// <summary>
    /// Charge point alarms (raised from the chargers' faults, offline and boot loop checks). Reading needs ViewAlarms,
    /// acknowledging, assigning and resolving EditAlarms.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AlarmController : ControllerBase
    {
        private const int ExportLimit = 50_000;

        private readonly VoltaXApiDbContext _db;
        private readonly IAlarmService _alarms;

        public AlarmController(VoltaXApiDbContext db, IAlarmService alarms)
        {
            _db = db;
            _alarms = alarms;
        }

        /// <summary>Alarms matching the filter (default: the active ones, latest first), one page.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAlarms([FromQuery] AlarmFilter filter, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);
            var query = AlarmQueries.Apply(_db.Alarms.AsNoTracking(), filter, this.User().ID);
            var total = await query.CountAsync(cancellationToken);
            var items = await AlarmQueries.Project(query.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(cancellationToken);
            return Ok(new PagedResult<AlarmDto>(items, total, page, pageSize));
        }

        /// <summary>Counts of the active alarms (by severity, unassigned, assigned to the caller), for badges.</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetAlarmSummary(CancellationToken cancellationToken)
        {
            var userId = this.User().ID;
            var active = await AlarmQueries.Apply(_db.Alarms.AsNoTracking(), new AlarmFilter(), userId)
                .Select(a => new { a.Severity, a.AssignedToID })
                .ToListAsync(cancellationToken);
            return Ok(new AlarmSummaryDto(active.Count,
                active.Count(a => a.Severity == AlarmSeverity.Critical), active.Count(a => a.Severity == AlarmSeverity.Major),
                active.Count(a => a.Severity == AlarmSeverity.Minor), active.Count(a => a.Severity == AlarmSeverity.Warning),
                active.Count(a => a.AssignedToID == null), active.Count(a => a.AssignedToID == userId)));
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetAlarm(long id, CancellationToken cancellationToken) =>
            await AlarmQueries.FindAsync(_db, id, cancellationToken) is { } alarm ? Ok(alarm) : NotFound();

        /// <summary>Who an alarm can be assigned to: administrators and users allowed to edit alarms.</summary>
        [HttpGet("assignees")]
        public async Task<IActionResult> GetAlarmAssignees(CancellationToken cancellationToken) =>
            Ok(await AlarmQueries.Assignees(_db).OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Select(u => new AlarmAssigneeDto(u.ID, u.FirstName + " " + u.LastName, u.Email))
                .ToListAsync(cancellationToken));

        /// <summary>The filtered alarms as a CSV file, streamed (at most 50,000 rows).</summary>
        [HttpGet("export")]
        public IActionResult ExportAlarms([FromQuery] AlarmFilter filter, [FromServices] IBusinessClock clock)
        {
            var rows = AlarmQueries.Project(AlarmQueries.Apply(_db.Alarms.AsNoTracking(), filter, this.User().ID).Take(ExportLimit)).AsAsyncEnumerable();
            string Local(DateTime? utc) => utc.HasValue ? clock.ToLocal(utc.Value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : "";
            return new CsvStreamResult<AlarmDto>($"eveon-alarms-{DateTime.UtcNow:yyyyMMdd}.csv",
                new[] { "Alarm", "Severity", "Status", "Type", "Code", "Description", "Details", "Station", "Charge point", "EVSE", "Connector",
                    "First seen", "Last seen", "Count", "Acknowledged", "Acknowledged by", "Assigned to", "Resolved", "Resolved by", "Resolution", "Cleared" },
                rows, a => new[]
                {
                    a.ID.ToString(CultureInfo.InvariantCulture), a.Severity.ToString(), a.Status.ToString(), a.Type.ToString(), a.Code, a.Description, a.Details,
                    a.StationName, a.ChargePointIdentity, a.EvseId?.ToString(CultureInfo.InvariantCulture), a.ConnectorNumber?.ToString(CultureInfo.InvariantCulture),
                    Local(a.FirstSeenAt), Local(a.LastSeenAt), a.Count.ToString(CultureInfo.InvariantCulture), Local(a.AcknowledgedAt), a.AcknowledgedBy,
                    a.AssignedTo, Local(a.ResolvedAt), a.ResolvedBy, a.ResolutionComment, Local(a.ClearedAt)
                });
        }

        [HttpPost("{id:long}/acknowledge")]
        public async Task<IActionResult> AcknowledgeAlarm(long id, [FromServices] IAuditLogger audit, CancellationToken cancellationToken)
        {
            var result = await _alarms.AcknowledgeAsync(id, this.User().ID, cancellationToken);
            if (result.Outcome == AlarmActionOutcome.Done) await audit.LogAsync("Alarm.Acknowledge", "Alarm", id.ToString(CultureInfo.InvariantCulture));
            return await Answer(result, cancellationToken);
        }

        [HttpPost("{id:long}/assign")]
        public async Task<IActionResult> AssignAlarm(long id, [FromBody] AssignAlarmDto body, [FromServices] IAuditLogger audit, CancellationToken cancellationToken)
        {
            var result = await _alarms.AssignAsync(id, body.AssignedToID, this.User().ID, cancellationToken);
            if (result.Outcome == AlarmActionOutcome.Done)
                await audit.LogAsync("Alarm.Assign", "Alarm", id.ToString(CultureInfo.InvariantCulture), new { body.AssignedToID });
            return await Answer(result, cancellationToken);
        }

        [HttpPost("{id:long}/resolve")]
        public async Task<IActionResult> ResolveAlarm(long id, [FromBody] ResolveAlarmDto body, [FromServices] IAuditLogger audit, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(body.Comment) || body.Comment.Trim().Length < 3)
                return BadRequest(new { error = "A resolution comment is required." });
            var result = await _alarms.ResolveAsync(id, this.User().ID, body.Comment, cancellationToken);
            if (result.Outcome == AlarmActionOutcome.Done) await audit.LogAsync("Alarm.Resolve", "Alarm", id.ToString(CultureInfo.InvariantCulture));
            return await Answer(result, cancellationToken);
        }

        private async Task<IActionResult> Answer(AlarmActionResult result, CancellationToken cancellationToken) => result.Outcome switch
        {
            AlarmActionOutcome.NotFound => NotFound(),
            AlarmActionOutcome.Closed => Conflict(new { error = "The alarm is already closed." }),
            AlarmActionOutcome.InvalidAssignee => BadRequest(new { error = "This user cannot be assigned alarms." }),
            _ => Ok(await AlarmQueries.FindAsync(_db, result.Alarm!.ID, cancellationToken))
        };
    }
}
