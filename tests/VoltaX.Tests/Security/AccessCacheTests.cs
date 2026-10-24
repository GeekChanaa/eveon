using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaX.Tests.Security;

/// <summary>
/// Access is cached a few seconds per user; every change of the user, a role or a permission must still apply to
/// the very next request.
/// </summary>
public class AccessCacheTests : IAsyncLifetime
{
    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly ApiFactory _api = new();
    private User _admin = null!, _customer = null!;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _admin = await Seeds.User(db, "admin@test.ma", role: "Admin");
            _customer = await Seeds.User(db, "customer@test.ma");
            foreach (var name in new[] { "AccessDashboard", "ViewUsers" })
                db.Permissions.Add(new Permission { Name = name });
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private AccessSnapshotCache Cache => _api.Services.GetRequiredService<AccessSnapshotCache>();

    private static async Task<string> Role(HttpClient client) =>
        JsonDocument.Parse(await client.GetStringAsync("/api/access/me")).RootElement.GetProperty("role").GetString()!;

    [Fact]
    public async Task Repeated_requests_are_served_from_the_cache()
    {
        var client = _api.Client(_customer);

        Assert.Equal("Customer", await Role(client));
        Assert.True(Cache.TryGet(_customer.ID, out var first));
        Assert.Equal("Customer", await Role(client));
        Assert.True(Cache.TryGet(_customer.ID, out var second));
        Assert.Same(first, second);
    }

    [Fact]
    public async Task A_role_change_applies_to_the_next_request()
    {
        var customer = _api.Client(_customer);
        Assert.Equal("Customer", await Role(customer));
        var partnerRole = await _api.Db(db => Seeds.Role(db, "Partner"));

        var response = await _api.Client(_admin).PutAsJsonAsync($"/api/access/users/{_customer.ID}/role", new { roleId = partnerRole.ID });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Partner", await Role(customer));
    }

    [Fact]
    public async Task A_suspension_saved_through_the_context_applies_to_the_next_request()
    {
        var customer = _api.Client(_customer);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/access/me")).StatusCode);

        await _api.Seed(async db => (await db.Users.SingleAsync(u => u.ID == _customer.ID)).SuspendedAt = DateTime.UtcNow.AddDays(1));

        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/access/me")).StatusCode);
        Assert.False(Cache.TryGet(_customer.ID, out _));
    }

    [Fact]
    public async Task A_role_permission_change_applies_to_every_user_of_the_role()
    {
        var operatorRole = await _api.Db(async db => await Seeds.Role(db, "Support"));
        var member = await _api.Db(db => Seeds.User(db, "support@test.ma", role: "Support"));
        var client = _api.Client(member);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/User/GetUserNames")).StatusCode);

        var permissionIds = await _api.Db(db => db.Permissions.Where(p => p.Name == "AccessDashboard" || p.Name == "ViewUsers").Select(p => p.ID).ToListAsync());
        var response = await _api.Client(_admin).PutAsJsonAsync($"/api/Role/UpdateRolePermissions/{operatorRole.ID}", permissionIds);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var me = JsonDocument.Parse(await client.GetStringAsync("/api/access/me")).RootElement;
        Assert.Contains("ViewUsers", me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    public async Task A_revocation_published_by_another_instance_drops_the_cached_access()
    {
        var customer = _api.Client(_customer);
        await Role(customer);
        Assert.True(Cache.TryGet(_customer.ID, out _));

        _api.Services.GetRequiredService<HubConnections>().HandleRemoteRevocation(
            JsonSerializer.Serialize(new { origin = "other-instance", userIds = new[] { _customer.ID } }));

        Assert.False(Cache.TryGet(_customer.ID, out _));
    }

    [Fact]
    public void Entries_expire_and_a_load_that_raced_an_invalidation_is_not_kept()
    {
        var time = new ManualTime();
        var cache = new AccessSnapshotCache(time, TimeSpan.FromSeconds(10));
        var snapshot = new AccessSnapshot(new User { ID = 7, Role = new Role { Name = "Customer" } }, new HashSet<string>());

        cache.Set(7, snapshot, cache.Current(7));
        Assert.True(cache.TryGet(7, out _));
        time.Now += TimeSpan.FromSeconds(10);
        Assert.False(cache.TryGet(7, out _));

        var stamp = cache.Current(7);
        cache.Invalidate(new[] { 7 });
        cache.Set(7, snapshot, stamp);
        Assert.False(cache.TryGet(7, out _));

        stamp = cache.Current(7);
        cache.Set(7, snapshot, stamp);
        cache.InvalidateAll();
        Assert.False(cache.TryGet(7, out _));

        var off = new AccessSnapshotCache(time, TimeSpan.Zero);
        off.Set(7, snapshot, off.Current(7));
        Assert.False(off.TryGet(7, out _));
    }
}
