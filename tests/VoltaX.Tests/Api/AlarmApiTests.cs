using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Models.Alarms;

namespace VoltaX.Tests.Api;

/// <summary>The operator's alarm list: filters, summary, export, acknowledge / assign / resolve and who may do it.</summary>
public class AlarmApiTests : IClassFixture<AlarmApiTests.Fixture>
{
    public sealed class Fixture : IAsyncLifetime
    {
        public ApiFactory Api { get; } = new();
        public User Admin { get; private set; } = null!;
        public User Technician { get; private set; } = null!;
        public User Viewer { get; private set; } = null!;
        public User Customer { get; private set; } = null!;
        public int MarinaId, AgdalId;

        public async Task InitializeAsync()
        {
            await Api.Seed(async db =>
            {
                Admin = await Seeds.User(db, "admin@alarms.test", role: "Admin");
                Customer = await Seeds.User(db, "driver@alarms.test");
                Technician = await Seeds.User(db, "tech@alarms.test", role: "Technician");
                Viewer = await Seeds.User(db, "viewer@alarms.test", role: "Supervisor");
                await Grant(db, "Technician", "AccessDashboard", "ViewAlarms", "EditAlarms");
                await Grant(db, "Supervisor", "AccessDashboard", "ViewAlarms");

                var marina = await Seeds.Station(db, name: "Marina");
                var agdal = await Seeds.Station(db, name: "Agdal");
                MarinaId = marina.ID;
                AgdalId = agdal.ID;
                var cp1 = await Seeds.ChargePoint(db, "CP-AL-1", c => { }, marina.ID);
                var cp2 = await Seeds.ChargePoint(db, "CP-AL-2", c => { }, agdal.ID);

                var t0 = Seeds.Now;
                db.Alarms.AddRange(
                    Alarm(cp1, AlarmType.ConnectorFault, "GroundFailure", AlarmSeverity.Critical, AlarmStatus.Open, t0.AddHours(-2), t0.AddHours(-1), evse: 1),
                    Alarm(cp1, AlarmType.ConnectorFault, "ReaderFailure", AlarmSeverity.Minor, AlarmStatus.Open, t0.AddHours(-5), t0.AddHours(-4), evse: 1),
                    Alarm(cp2, AlarmType.Offline, "Offline", AlarmSeverity.Critical, AlarmStatus.Acknowledged, t0.AddMinutes(-30), t0.AddMinutes(-30)),
                    Alarm(cp2, AlarmType.BootLoop, "BootLoop", AlarmSeverity.Major, AlarmStatus.Cleared, t0.AddDays(-3), t0.AddDays(-3)),
                    Alarm(cp1, AlarmType.DeviceEvent, "Connector.Problem", AlarmSeverity.Major, AlarmStatus.Resolved, t0.AddDays(-10), t0.AddDays(-9), evse: 1));
            });
        }

        private static Alarm Alarm(ChargePoint cp, AlarmType type, string code, AlarmSeverity severity, AlarmStatus status, DateTime first, DateTime last, int? evse = null)
        {
            var key = VoltaXApi.Models.Alarms.Alarm.KeyOf(type, cp.ID, evse, evse == null ? null : 1, code);
            return new Alarm
            {
                Type = type, Code = code, Severity = severity, Status = status, Description = $"{code} on {cp.ChargePointId}",
                ChargePointID = cp.ID, ChargePointIdentity = cp.ChargePointId, EvseId = evse, ConnectorNumber = evse == null ? null : 1,
                FirstSeenAt = first, LastSeenAt = last, Count = 1, DedupKey = key,
                OpenKey = status is AlarmStatus.Open or AlarmStatus.Acknowledged ? key : key + "#" + Guid.NewGuid().ToString("N"),
                CreatedAt = first, UpdatedAt = last
            };
        }

        private static async Task Grant(VoltaXApiDbContext db, string role, params string[] permissions)
        {
            var roleId = (await Seeds.Role(db, role)).ID;
            foreach (var name in permissions)
            {
                var permission = await db.Permissions.FirstAsync(p => p.Name == name);
                db.RolePermissions.Add(new RolePermission { RoleID = roleId, PermissionID = permission.ID, Scope = PermissionScope.Global });
            }
            await db.SaveChangesAsync();
        }

        public Task DisposeAsync()
        {
            Api.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly Fixture _f;

    public AlarmApiTests(Fixture fixture) => _f = fixture;

    private HttpClient Admin => _f.Api.Client(_f.Admin);

    private async Task<long> Id(string code) =>
        await _f.Api.Db(db => db.Alarms.Where(a => a.Code == code).Select(a => a.ID).FirstAsync());

    [Fact]
    public async Task The_list_shows_the_active_alarms_latest_first_by_default()
    {
        var page = await Admin.GetFromJsonAsync<JsonElement>("/api/Alarm");
        var items = page.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(new[] { "Offline", "GroundFailure", "ReaderFailure" }, items.Select(i => i.GetProperty("code").GetString()));
        var fault = items[1];
        Assert.Equal("Critical", fault.GetProperty("severity").GetString());
        Assert.Equal("Open", fault.GetProperty("status").GetString());
        Assert.Equal("ConnectorFault", fault.GetProperty("type").GetString());
        Assert.Equal("Marina", fault.GetProperty("stationName").GetString());
        Assert.Equal("CP-AL-1", fault.GetProperty("chargePointIdentity").GetString());
        Assert.Equal(1, fault.GetProperty("evseId").GetInt32());
    }

    [Theory]
    [InlineData("status=all", 5)]
    [InlineData("status=closed", 2)]
    [InlineData("status=acknowledged", 1)]
    [InlineData("severity=Critical", 2)]
    [InlineData("type=Offline", 1)]
    [InlineData("search=Agdal", 1)]
    [InlineData("search=ground", 1)]
    [InlineData("status=all&from=2026-10-19", 3)]
    [InlineData("status=all&to=2026-10-12", 1)]
    [InlineData("unassigned=true", 3)]
    public async Task Alarms_can_be_filtered(string query, int expected)
    {
        var page = await Admin.GetFromJsonAsync<JsonElement>("/api/Alarm?" + query);
        Assert.Equal(expected, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Alarms_can_be_filtered_by_station_and_sorted_by_severity()
    {
        var marina = await Admin.GetFromJsonAsync<JsonElement>($"/api/Alarm?chargingStationID={_f.MarinaId}&sort=severity");
        var codes = marina.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToList();
        Assert.Equal(new[] { "GroundFailure", "ReaderFailure" }, codes);
    }

    [Fact]
    public async Task The_summary_counts_the_active_alarms_by_severity()
    {
        var summary = await Admin.GetFromJsonAsync<JsonElement>("/api/Alarm/summary");
        Assert.Equal(3, summary.GetProperty("active").GetInt32());
        Assert.Equal(2, summary.GetProperty("critical").GetInt32());
        Assert.Equal(1, summary.GetProperty("minor").GetInt32());
    }

    [Fact]
    public async Task The_export_streams_the_filtered_alarms_as_csv()
    {
        var response = await Admin.GetAsync("/api/Alarm/export?status=all&type=ConnectorFault");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("eveon-alarms-", response.Content.Headers.ContentDisposition!.FileName);
        var lines = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync()).TrimStart('﻿').Trim().Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("Alarm;Severity;Status;Type;Code", lines[0]);
        Assert.Contains(";Critical;Open;ConnectorFault;GroundFailure;", lines[1]);
        Assert.Contains(";Marina;CP-AL-1;1;1;", lines[1]);
    }

    [Fact]
    public async Task A_technician_acknowledges_takes_and_resolves_an_alarm_with_a_comment()
    {
        await _f.Api.Seed(async db =>
        {
            var cp = await db.ChargePoints.FirstAsync(c => c.ChargePointId == "CP-AL-1");
            var key = Alarm.KeyOf(AlarmType.ConnectorFault, cp.ID, 2, 1, "OverVoltage");
            db.Alarms.Add(new Alarm
            {
                Type = AlarmType.ConnectorFault, Code = "OverVoltage", Severity = AlarmSeverity.Major, Status = AlarmStatus.Open, Description = "Over voltage",
                ChargePointID = cp.ID, ChargePointIdentity = cp.ChargePointId, EvseId = 2, ConnectorNumber = 1, FirstSeenAt = Seeds.Now, LastSeenAt = Seeds.Now,
                DedupKey = key, OpenKey = key, CreatedAt = Seeds.Now, UpdatedAt = Seeds.Now
            });
        });
        var id = await Id("OverVoltage");
        var tech = _f.Api.Client(_f.Technician);
        try
        {

        var acknowledged = await (await tech.PostAsync($"/api/Alarm/{id}/acknowledge", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Acknowledged", acknowledged.GetProperty("status").GetString());
        Assert.Equal("Ev Driver", acknowledged.GetProperty("acknowledgedBy").GetString());

        var assigned = await tech.PostAsJsonAsync($"/api/Alarm/{id}/assign", new { assignedToID = _f.Technician.ID });
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal(_f.Technician.ID, (await assigned.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("assignedToID").GetInt32());
        var mine = await tech.GetFromJsonAsync<JsonElement>("/api/Alarm?assignedToMe=true");
        Assert.Equal(1, mine.GetProperty("totalCount").GetInt32());

        var noComment = await tech.PostAsJsonAsync($"/api/Alarm/{id}/resolve", new { comment = "" });
        Assert.Equal(HttpStatusCode.BadRequest, noComment.StatusCode);

        var resolved = await tech.PostAsJsonAsync($"/api/Alarm/{id}/resolve", new { comment = "Supply voltage fixed by the utility" });
        var body = await resolved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Resolved", body.GetProperty("status").GetString());
        Assert.Equal("Supply voltage fixed by the utility", body.GetProperty("resolutionComment").GetString());

        var again = await tech.PostAsync($"/api/Alarm/{id}/acknowledge", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        var audit = await _f.Api.Db(db => db.AuditLogs.Where(a => a.Action.StartsWith("Alarm.")).Select(a => a.Action).ToListAsync());
        Assert.Contains("Alarm.Resolve", audit);
        }
        finally
        {
            // Leave the shared alarm set as the other tests expect it.
            await _f.Api.Db(db => db.Alarms.Where(a => a.ID == id).ExecuteDeleteAsync());
        }
    }

    [Fact]
    public async Task Alarms_are_only_assigned_to_users_who_can_handle_them()
    {
        var id = await Id("ReaderFailure");
        var assignees = await Admin.GetFromJsonAsync<JsonElement>("/api/Alarm/assignees");
        var ids = assignees.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(_f.Technician.ID, ids);
        Assert.Contains(_f.Admin.ID, ids);
        Assert.DoesNotContain(_f.Viewer.ID, ids);
        Assert.DoesNotContain(_f.Customer.ID, ids);

        var toCustomer = await Admin.PostAsJsonAsync($"/api/Alarm/{id}/assign", new { assignedToID = _f.Customer.ID });
        Assert.Equal(HttpStatusCode.BadRequest, toCustomer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.PostAsync("/api/Alarm/999999/acknowledge", null)).StatusCode);
    }

    [Fact]
    public async Task A_viewer_sees_alarms_but_cannot_act_and_customers_see_nothing()
    {
        var id = await Id("GroundFailure");
        var viewer = _f.Api.Client(_f.Viewer);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/Alarm")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/Alarm/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsync($"/api/Alarm/{id}/acknowledge", null)).StatusCode);

        var customer = _f.Api.Client(_f.Customer);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/Alarm")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/Alarm/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.Api.Client().GetAsync("/api/Alarm/summary")).StatusCode);
    }
}
