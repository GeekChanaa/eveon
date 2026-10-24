using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Security;

/// <summary>
/// Request bodies only ever set the plain fields a client may change: never ids, ownership, balances, roles,
/// credentials, and never rows of other tables through nested (navigation) objects.
/// </summary>
public class MassAssignmentTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _admin = null!, _customer = null!, _other = null!;
    private int _typeId, _cardId, _otherSettingId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _admin = await Seeds.User(db, "admin@test.ma", role: "Admin");
            _customer = await Seeds.User(db, "me@test.ma");
            _other = await Seeds.User(db, "other@test.ma");
            var type = new NotificationType { Name = "Low balance", Description = "", ForCustomers = true };
            db.NotificationTypes.Add(type);
            var card = Seeds.Card("MA-0001", 10m, _customer.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            var otherSetting = new NotificationSetting { UserID = _other.ID, NotificationTypeID = type.ID, Active = true, Email = true };
            db.NotificationSettings.Add(otherSetting);
            await db.SaveChangesAsync();
            _typeId = type.ID;
            _cardId = card.ID;
            _otherSettingId = otherSetting.ID;
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private Task<int> Users() => _api.Db(db => db.Users.IgnoreQueryFilters().CountAsync());

    [Fact]
    public async Task Saving_a_notification_setting_ignores_nested_objects_and_ids()
    {
        var types = await _api.Db(db => db.NotificationTypes.CountAsync());

        // notificationType binds cleanly (no required navigation): without the fix it would insert a new type row.
        var response = await _api.Client(_customer).PostAsJsonAsync("/api/NotificationSetting/SaveUserNotificationSetting", new
        {
            userID = _customer.ID,
            notificationTypeID = _typeId,
            active = true,
            email = false,
            notificationType = new { name = "Nested type", description = "" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(types, await _api.Db(db => db.NotificationTypes.CountAsync()));
        var mine = await _api.Db(db => db.NotificationSettings.SingleAsync(s => s.UserID == _customer.ID));
        Assert.True(mine.Active);
        Assert.Equal(_typeId, mine.NotificationTypeID);
    }

    [Fact]
    public async Task A_notification_setting_cannot_take_over_the_id_of_someone_else_s_row()
    {
        var other = new NotificationType { Name = "Session ended", Description = "", ForCustomers = true };
        await _api.Seed(db => { db.NotificationTypes.Add(other); return Task.CompletedTask; });

        await _api.Client(_customer).PostAsJsonAsync("/api/NotificationSetting/SaveUserNotificationSetting",
            new { id = _otherSettingId, userID = _customer.ID, notificationTypeID = other.ID, active = false, email = false });

        var theirs = await _api.Db(db => db.NotificationSettings.AsNoTracking().SingleAsync(s => s.ID == _otherSettingId));
        Assert.Equal(_other.ID, theirs.UserID);
        Assert.True(theirs.Active);
    }

    [Fact]
    public async Task Creating_a_system_report_ignores_nested_objects()
    {
        var users = await Users();

        // card binds cleanly (no required navigation): without the fix it would insert a new card row with a balance.
        var response = await _api.Client(_admin).PostAsJsonAsync("/api/SystemReport/CreateSystemReport", new
        {
            reportCategory = "Technical",
            issueDescription = "Display broken",
            card = new { cardNumber = "NESTED-1", balance = 1000, note = "" }
        });

        Assert.True(response.IsSuccessStatusCode, response.StatusCode + ": " + await response.Content.ReadAsStringAsync());
        Assert.Equal(users, await Users());
        Assert.False(await _api.Db(db => db.Cards.AnyAsync(c => c.CardNumber == "NESTED-1")));
        Assert.True(await _api.Db(db => db.SystemReports.AnyAsync(r => r.IssueDescription == "Display broken")));
    }

    [Fact]
    public async Task Generic_updates_never_change_balances_ownership_or_credentials()
    {
        var response = await _api.Client(_admin).PutAsJsonAsync($"/api/Card/{_cardId}", new
        {
            note = "updated",
            balance = 99999,
            balanceVersion = 42,
            userID = _other.ID,
            isDeleted = true
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var card = await _api.Db(db => db.Cards.IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.ID == _cardId));
        Assert.Equal("updated", card.Note);
        Assert.Equal(10m, card.Balance);
        Assert.Equal(_customer.ID, card.UserID);
        Assert.False(card.IsDeleted);
    }

    [Fact]
    public async Task Raw_user_crud_is_closed_even_to_administrators()
    {
        var admin = _api.Client(_admin);
        var hash = Convert.ToBase64String(new byte[64]);

        var update = await admin.PutAsJsonAsync($"/api/User/{_customer.ID}", new { roleID = 1, passwordHash = hash, passwordSalt = hash, partnerID = 1 });
        var create = await admin.PostAsJsonAsync("/api/User", new { email = "raw@test.ma", firstName = "R", lastName = "R", roleID = 1 });
        var read = await admin.GetAsync($"/api/User/{_customer.ID}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.False(create.IsSuccessStatusCode, create.StatusCode.ToString()); // 400 (body validation runs first) or 403
        Assert.False(await _api.Db(db => db.Users.AnyAsync(u => u.Email == "raw@test.ma")));
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
    }

    [Theory]
    [InlineData("OrderBy=it.User.PasswordHash")]
    [InlineData("OrderBy=Balance,%20User.PasswordHash")]
    [InlineData("OrderBy=np(User.PasswordHash)")]
    [InlineData("OrderBy=Balance&ReverseOrder=desc;DROP")]
    [InlineData("FilterBy=User-PasswordHash&FilterValue=AA")]
    [InlineData("FilterBy=User-ResetPasswordToken&FilterValue=AA")]
    [InlineData("FilterBy=Balance%20%3E%200%20||%20true&FilterValue=1")]
    [InlineData("FilterBy=Balance&FilterValue=1&FilterMethod=)||(true")]
    [InlineData("SearchBy=CardNumber)||(true&SearchValue=x")]
    [InlineData("SearchBy=User.Password&SearchValue=x")]
    [InlineData("SearchBy=User.Role.Name&SearchValue=Admin")]
    public async Task Dynamic_list_queries_refuse_expressions_and_credential_fields(string query)
    {
        var response = await _api.Client(_admin).GetAsync("/api/Card?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dynamic_list_queries_bind_values_as_parameters()
    {
        var injected = await _api.Client(_admin).GetFromJsonAsync<System.Text.Json.JsonElement>(
            "/api/Card?SearchBy=CardNumber&SearchValue=" + Uri.EscapeDataString("\") || (1 == 1"));
        var normal = await _api.Client(_admin).GetFromJsonAsync<System.Text.Json.JsonElement>("/api/Card?SearchBy=CardNumber&SearchValue=MA-0001");

        Assert.Equal(0, injected.GetArrayLength());
        Assert.Equal(1, normal.GetArrayLength());
    }
}
