using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Models.Alarms;

namespace VoltaXApi.Services.Alarms
{
    /// <summary>An alarm as the dashboard shows it (list rows, details, realtime pushes).</summary>
    public sealed record AlarmDto(
        long ID,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] AlarmType Type,
        string Code,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] AlarmSeverity Severity,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] AlarmStatus Status,
        string Description, string? Details,
        int ChargePointID, string ChargePointIdentity, int? ChargingStationID, string? StationName,
        int? EvseId, int? ConnectorNumber, int? ConnectorID,
        DateTime FirstSeenAt, DateTime LastSeenAt, int Count,
        DateTime? AcknowledgedAt, string? AcknowledgedBy, int? AssignedToID, string? AssignedTo, DateTime? AssignedAt,
        DateTime? ResolvedAt, string? ResolvedBy, string? ResolutionComment, DateTime? ClearedAt);

    public sealed record AlarmAssigneeDto(int ID, string Name, string Email);

    public sealed record AlarmSummaryDto(int Active, int Critical, int Major, int Minor, int Warning, int Unassigned, int AssignedToMe);

    /// <summary>Query string filter of the alarm list and export.</summary>
    public sealed class AlarmFilter
    {
        /// <summary>active (default: open or acknowledged), open, acknowledged, resolved, cleared, closed, all.</summary>
        public string? Status { get; set; }
        public AlarmSeverity? Severity { get; set; }
        public AlarmType? Type { get; set; }
        public int? ChargingStationID { get; set; }
        public int? ChargePointID { get; set; }
        public int? AssignedToID { get; set; }
        public bool AssignedToMe { get; set; }
        public bool Unassigned { get; set; }
        /// <summary>Alarms active at some point of [From, To] (first seen before To, last seen after From).</summary>
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }
        /// <summary>lastSeen (default, newest first), severity (most severe first), firstSeen, count.</summary>
        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public static class AlarmQueries
    {
        /// <summary>Dashboard users who can take an alarm: administrators and users allowed to edit alarms.</summary>
        public static IQueryable<User> Assignees(VoltaXApiDbContext db)
        {
            var now = DateTime.UtcNow;
            return db.Users.AsNoTracking()
                .Where(u => !u.IsDeleted && (u.SuspendedAt == null || u.SuspendedAt <= now) && u.Role != null && !u.Role.IsDeleted)
                .Where(u => u.Role.Name == "Admin"
                    || u.Role.RolePermissions.Any(rp => !rp.IsDeleted && !rp.Permission.IsDeleted && rp.Scope == PermissionScope.Global && rp.Permission.Name == "AccessDashboard")
                    && u.Role.RolePermissions.Any(rp => !rp.IsDeleted && !rp.Permission.IsDeleted && rp.Scope == PermissionScope.Global && rp.Permission.Name == nameof(PermissionEnum.EditAlarms)));
        }

        public static IQueryable<Alarm> Apply(IQueryable<Alarm> query, AlarmFilter filter, int currentUserId)
        {
            query = (filter.Status ?? "active").ToLowerInvariant() switch
            {
                "open" => query.Where(a => a.Status == AlarmStatus.Open),
                "acknowledged" => query.Where(a => a.Status == AlarmStatus.Acknowledged),
                "resolved" => query.Where(a => a.Status == AlarmStatus.Resolved),
                "cleared" => query.Where(a => a.Status == AlarmStatus.Cleared),
                "closed" => query.Where(a => a.Status == AlarmStatus.Resolved || a.Status == AlarmStatus.Cleared),
                "all" => query,
                _ => query.Where(a => a.Status == AlarmStatus.Open || a.Status == AlarmStatus.Acknowledged)
            };
            if (filter.Severity.HasValue) query = query.Where(a => a.Severity == filter.Severity);
            if (filter.Type.HasValue) query = query.Where(a => a.Type == filter.Type);
            if (filter.ChargePointID.HasValue) query = query.Where(a => a.ChargePointID == filter.ChargePointID);
            if (filter.ChargingStationID.HasValue) query = query.Where(a => a.ChargePoint!.ChargingStationID == filter.ChargingStationID);
            if (filter.AssignedToMe) query = query.Where(a => a.AssignedToID == currentUserId);
            else if (filter.Unassigned) query = query.Where(a => a.AssignedToID == null);
            else if (filter.AssignedToID.HasValue) query = query.Where(a => a.AssignedToID == filter.AssignedToID);
            if (filter.From.HasValue) { var from = filter.From.Value.ToUniversalTime(); query = query.Where(a => a.LastSeenAt >= from); }
            if (filter.To.HasValue) { var to = filter.To.Value.ToUniversalTime(); query = query.Where(a => a.FirstSeenAt <= to); }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                // LIKE: case-insensitive on SQL Server, MySQL and SQLite alike.
                // Wildcards are dropped (their escape syntax differs between providers).
                var pattern = "%" + new string(filter.Search.Trim().Where(c => c is not ('%' or '_' or '[' or ']')).ToArray()) + "%";
                query = query.Where(a => EF.Functions.Like(a.ChargePointIdentity, pattern) || EF.Functions.Like(a.Code, pattern)
                                         || EF.Functions.Like(a.Description, pattern) || EF.Functions.Like(a.ChargePoint!.ChargingStation!.Name, pattern));
            }
            return (filter.Sort ?? "").ToLowerInvariant() switch
            {
                "severity" => query.OrderByDescending(a => a.Severity).ThenByDescending(a => a.LastSeenAt).ThenByDescending(a => a.ID),
                "firstseen" => query.OrderByDescending(a => a.FirstSeenAt).ThenByDescending(a => a.ID),
                "count" => query.OrderByDescending(a => a.Count).ThenByDescending(a => a.LastSeenAt).ThenByDescending(a => a.ID),
                _ => query.OrderByDescending(a => a.LastSeenAt).ThenByDescending(a => a.ID)
            };
        }

        public static IQueryable<AlarmDto> Project(IQueryable<Alarm> query) =>
            query.Select(a => new AlarmDto(
                a.ID, a.Type, a.Code, a.Severity, a.Status, a.Description, a.Details,
                a.ChargePointID, a.ChargePointIdentity, a.ChargePoint!.ChargingStationID, a.ChargePoint.ChargingStation!.Name,
                a.EvseId, a.ConnectorNumber, a.ConnectorID,
                a.FirstSeenAt, a.LastSeenAt, a.Count,
                a.AcknowledgedAt, a.AcknowledgedBy == null ? null : a.AcknowledgedBy.FirstName + " " + a.AcknowledgedBy.LastName,
                a.AssignedToID, a.AssignedTo == null ? null : a.AssignedTo.FirstName + " " + a.AssignedTo.LastName, a.AssignedAt,
                a.ResolvedAt, a.ResolvedBy == null ? null : a.ResolvedBy.FirstName + " " + a.ResolvedBy.LastName, a.ResolutionComment, a.ClearedAt));

        public static Task<AlarmDto?> FindAsync(VoltaXApiDbContext db, long id, CancellationToken cancellationToken = default) =>
            Project(db.Alarms.AsNoTracking().IgnoreQueryFilters().Where(a => a.ID == id)).FirstOrDefaultAsync(cancellationToken);
    }
}
