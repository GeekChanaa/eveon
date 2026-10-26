using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Api;

/// <summary>The partner account an administrator creates from a partner's email; its password only goes to the administrators.</summary>
public class PartnerUserApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _admin = null!, _operator = null!;
    private Partner _partner = null!;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _admin = await Seeds.User(db, "admin@test.ma", role: "Admin");
            _admin.IsEmailVerified = true;
            await Seeds.User(db, "unverified-admin@test.ma", role: "Admin");
            _operator = await Seeds.User(db, "ops@test.ma", role: "Operator");
            // Everything the generic rules ask for: only the administrator check stands in the way.
            var operatorRole = await Seeds.Role(db, "Operator");
            foreach (var name in new[] { "AccessDashboard", "ViewPartners", "CreatePartners", "EditPartners" })
            {
                var permission = await db.Permissions.FirstAsync(p => p.Name == name);
                db.RolePermissions.Add(new RolePermission { RoleID = operatorRole.ID, PermissionID = permission.ID, Scope = PermissionScope.Global });
            }
            await Seeds.Role(db, "Partner");
            _partner = new Partner { Name = "Acme Charge", Description = "", Email = " Contact@Acme.ma ", Phone = "1", PartnerIdentificationNumber = "P-ACME" };
            db.Partners.Add(_partner);
            await db.SaveChangesAsync();
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Creates_a_partner_account_and_mails_its_password_to_the_administrators()
    {
        var response = await _api.Client(_admin).PostAsync($"/api/Partner/CreatePartnerUser/{_partner.ID}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("contact@acme.ma", body.GetProperty("email").GetString());
        Assert.False(body.TryGetProperty("password", out _));

        var user = await _api.Db(db => db.Users.Include(u => u.Role).SingleAsync(u => u.Email == "contact@acme.ma"));
        Assert.Equal("Partner", user.Role.Name);
        Assert.Equal(_partner.ID, user.PartnerID);
        Assert.True(user.IsEmailVerified);

        var mail = Assert.Single(_api.Mail.CallsTo(nameof(IMailService.SendPartnerAccountCreatedMail)));
        // Verified administrators, plus SupportEmails:Admin from the configuration.
        var recipients = (IReadOnlyList<string>)mail[0]!;
        Assert.Contains("admin@test.ma", recipients);
        Assert.DoesNotContain("unverified-admin@test.ma", recipients);
        Assert.Equal("contact@acme.ma", mail[2]);
        Assert.True(AuthHelper.VerifyPasswordHash((string)mail[3]!, user.PasswordHash, user.PasswordSalt));
    }

    [Fact]
    public async Task A_second_account_on_the_same_email_is_refused()
    {
        var admin = _api.Client(_admin);

        await admin.PostAsync($"/api/Partner/CreatePartnerUser/{_partner.ID}", null);
        var again = await admin.PostAsync($"/api/Partner/CreatePartnerUser/{_partner.ID}", null);

        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal(1, await _api.Db(db => db.Users.CountAsync(u => u.Email == "contact@acme.ma")));
        Assert.Single(_api.Mail.CallsTo(nameof(IMailService.SendPartnerAccountCreatedMail)));
    }

    [Fact]
    public async Task Only_administrators_create_partner_accounts()
    {
        var response = await _api.Client(_operator).PostAsync($"/api/Partner/CreatePartnerUser/{_partner.ID}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(await _api.Db(db => db.Users.AnyAsync(u => u.Email == "contact@acme.ma")));
    }
}
