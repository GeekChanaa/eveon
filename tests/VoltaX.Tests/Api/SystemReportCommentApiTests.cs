using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Api;

/// <summary>Comments on system reports: the author is the signed-in operator, never a value sent by the browser.</summary>
public class SystemReportCommentApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _admin = null!, _other = null!;
    private int _reportId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _admin = await Seeds.User(db, "ops@test.ma", role: "Admin");
            _other = await Seeds.User(db, "someone@test.ma");
            var report = new SystemReport { IssueDescription = "Screen frozen", ReportCategory = default };
            db.SystemReports.Add(report);
            await db.SaveChangesAsync();
            _reportId = report.ID;
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task A_comment_is_written_by_the_caller_and_listed_in_order()
    {
        var ops = _api.Client(_admin);

        var first = await ops.PostAsJsonAsync($"/api/SystemReportComment/CreateComment/{_reportId}", new { content = "Rebooted the charger", userId = _other.ID });
        var second = await ops.PostAsJsonAsync($"/api/SystemReportComment/CreateComment/{_reportId}", new { content = "Fixed" });
        var list = await ops.GetFromJsonAsync<JsonElement>($"/api/SystemReportComment/GetSystemReportComments/{_reportId}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var stored = await _api.Db(db => db.SystemReportComments.OrderBy(c => c.ID).ToListAsync());
        Assert.All(stored, c => Assert.Equal(_admin.ID, c.UserID));
        Assert.Equal(new[] { "Rebooted the charger", "Fixed" }, list.EnumerateArray().Select(c => c.GetProperty("content").GetString()));
        Assert.Equal("Ev Driver", list[0].GetProperty("userName").GetString());
    }

    [Fact]
    public async Task Comments_cannot_be_created_or_rewritten_through_the_generic_endpoints()
    {
        var ops = _api.Client(_admin);

        var create = await ops.PostAsJsonAsync("/api/SystemReportComment", new { systemReportID = _reportId, content = "x", userID = _other.ID });
        var empty = await ops.PostAsJsonAsync($"/api/SystemReportComment/CreateComment/{_reportId}", new { content = "" });
        var unknown = await ops.PostAsJsonAsync("/api/SystemReportComment/CreateComment/999999", new { content = "Hello" });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(0, await _api.Db(db => db.SystemReportComments.CountAsync()));
    }
}
