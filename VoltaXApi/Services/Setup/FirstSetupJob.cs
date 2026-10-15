using System.Security.Cryptography;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Data.Seeders;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Setup;

/// <summary>
/// Runs on every start (so on every deploy) and makes sure the database holds what a fresh
/// installation needs: permissions, the Admin / Partner / Customer roles, the default OCPP
/// profile, notification types and reference data. It also checks the pricing defaults and keeps
/// the three test accounts usable. Every step is idempotent: existing data and admin edits win,
/// except for the role rules below, which are enforced.
///   Admin    every permission (and AccessSnapshot.IsAdmin bypasses checks anyway).
///   Partner  no permission: the partner portal is granted by role name + PartnerID, and without
///            AccessDashboard the admin dashboard stays closed.
///   Customer no permission: own data and the mobile / customer endpoints only.
/// </summary>
public sealed class FirstSetupJob(
    VoltaXApiDbContext db,
    ICardRepository cards,
    IMapper mapper,
    GlobalConfigurations pricing,
    IOptions<FirstSetupOptions> options,
    IWebHostEnvironment env,
    ILogger<FirstSetupJob> logger)
{
    public const string AdminRole = "Admin";
    public const string PartnerRole = "Partner";
    public const string CustomerRole = "Customer";
    private const string TestPartnerNumber = "EVEON-TEST";

    private readonly FirstSetupOptions _options = options.Value;
    private readonly FirstSetupResult _result = new();

    public async Task<FirstSetupResult> RunAsync()
    {
        await Step("Permissions", EnsurePermissions);
        await Step("Roles", EnsureRoles);
        await Step("OCPP default profile", EnsureOcppProfile);
        await Step("Notification types", EnsureNotificationTypes);
        await Step("Reference data", EnsureReferenceData);
        await Step("Pricing defaults", CheckPricing);
        if (_options.TestUsers.Enabled)
            await Step("Test users", EnsureTestUsers);

        foreach (var line in _result.Lines)
            logger.Log(line.Level, "First setup | {Step}: {Message}", line.Step, line.Message);
        if (_result.HasErrors)
            logger.LogError("First setup finished with errors, see the lines above");
        else
            logger.LogInformation("First setup check passed");
        return _result;
    }

    private async Task Step(string name, Func<string, Task> step)
    {
        try
        {
            await step(name);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            logger.LogError(ex, "First setup step {Step} failed", name);
            _result.Error(name, ex.Message);
        }
    }

    private async Task EnsurePermissions(string step)
    {
        var added = await PermissionSeeder.Seed(db);
        _result.Ok(step, added.Count == 0 ? "up to date" : $"added {added.Count}");
    }

    private async Task EnsureRoles(string step)
    {
        var now = DateTime.UtcNow;
        var roles = await db.Roles.IgnoreQueryFilters().Include(r => r.RolePermissions).ToListAsync();
        var permissions = await db.Permissions.ToListAsync();

        Role Ensure(string name)
        {
            var role = roles.Where(r => r.Name == name).OrderBy(r => r.IsDeleted).FirstOrDefault();
            if (role == null)
            {
                role = new Role { Name = name, CreatedAt = now, UpdatedAt = now };
                db.Roles.Add(role);
                _result.Ok(step, $"created role {name}");
            }
            else if (role.IsDeleted)
            {
                role.IsDeleted = false;
                role.UpdatedAt = now;
                _result.Warn(step, $"restored deleted role {name}");
            }
            return role;
        }

        var admin = Ensure(AdminRole);
        var granted = 0;
        foreach (var permission in permissions)
        {
            var link = admin.RolePermissions.FirstOrDefault(rp => rp.PermissionID == permission.ID && rp.Scope == PermissionScope.Global);
            if (link == null)
            {
                admin.RolePermissions.Add(new RolePermission { Permission = permission, Scope = PermissionScope.Global, CreatedAt = now, UpdatedAt = now });
                granted++;
            }
            else if (link.IsDeleted)
            {
                link.IsDeleted = false;
                link.UpdatedAt = now;
                granted++;
            }
        }
        if (granted > 0) _result.Ok(step, $"granted {granted} missing permission(s) to {AdminRole}");

        // Partner and Customer never hold permissions: any permission (AccessDashboard first) opens admin endpoints.
        foreach (var role in new[] { Ensure(PartnerRole), Ensure(CustomerRole) })
        {
            var revoked = role.RolePermissions.Where(rp => !rp.IsDeleted).ToList();
            foreach (var link in revoked)
            {
                link.IsDeleted = true;
                link.UpdatedAt = now;
            }
            if (revoked.Count > 0)
            {
                var names = permissions.Where(p => revoked.Any(rp => rp.PermissionID == p.ID)).Select(p => p.Name);
                _result.Warn(step, $"revoked {revoked.Count} permission(s) from {role.Name}: {string.Join(", ", names)}");
            }
        }

        await db.SaveChangesAsync();
        _result.Ok(step, $"{AdminRole} (all {permissions.Count} permissions), {PartnerRole} and {CustomerRole} (none) are in place");
    }

    private async Task EnsureOcppProfile(string step)
    {
        await OcppDefaultProfileSeeder.Seed(db);
        var count = await db.OcppDefaultVariables.CountAsync();
        if (count == 0) _result.Warn(step, "the profile is empty (every row was deleted from the dashboard)");
        else _result.Ok(step, $"{count} default variable(s)");
    }

    private async Task EnsureNotificationTypes(string step)
    {
        var defaults = new (string Name, string Description)[]
        {
            ("Default", "Default Notifications"), ("User", "User Notifications"),
            ("Charging Station", "Charging Station Notifications"), ("Charge Point", "Charge Point Notifications"),
            ("Connector", "Connector Notifications"), ("Charging Cards", "Charging Cards Notifications"),
            ("Transaction", "Transaction Notifications"), ("Recharge Orders", "Recharge Orders Notifications"),
            ("Comment", "Comment Notifications"),
        };
        var existing = await db.NotificationTypes.IgnoreQueryFilters().Select(t => t.Name).ToListAsync();
        var now = DateTime.UtcNow;
        var missing = defaults.Where(d => !existing.Contains(d.Name)).ToList();
        foreach (var (name, description) in missing)
            db.NotificationTypes.Add(new NotificationType { Name = name, Description = description, ForAdmins = true, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        _result.Ok(step, missing.Count == 0 ? "up to date" : $"added {string.Join(", ", missing.Select(m => m.Name))}");
    }

    private async Task EnsureReferenceData(string step)
    {
        var brandsFile = Path.Combine(env.ContentRootPath, "sql-scripts", "charger-brands.json");
        if (await db.ChargePointBrands.IgnoreQueryFilters().AnyAsync())
            _result.Ok(step, "charge point brands present");
        else if (File.Exists(brandsFile))
        {
            await ChargePointBrandsSeeder.Seed(db, mapper, brandsFile);
            _result.Ok(step, "seeded charge point brands and models");
        }
        else _result.Warn(step, $"no charge point brands and {brandsFile} is missing");

        var carsFile = Path.Combine(env.ContentRootPath, "sql-scripts", "EV-cars.csv");
        if (await db.ElectricVehicleModels.IgnoreQueryFilters().AnyAsync())
            _result.Ok(step, "electric vehicle models present");
        else if (File.Exists(carsFile))
        {
            await ElectricVehicleModelSeeder.SeedAsync(db, carsFile);
            _result.Ok(step, "seeded electric vehicle models");
        }
        else _result.Warn(step, $"no electric vehicle models and {carsFile} is missing");

        // Countries / states / cities are large SQL imports (sql-scripts/*.sql): run them by hand once.
        if (!await db.Countries.AnyAsync() || !await db.Cities.AnyAsync())
            _result.Warn(step, "countries or cities are empty: import sql-scripts/countries.sql, states.sql and cities.sql");
        else
            _result.Ok(step, "countries and cities present");
    }

    private Task CheckPricing(string step)
    {
        void Positive(string name, double value)
        {
            if (value <= 0) _result.Error(step, $"GlobalConfigurations:{name} is {value}, expected a value above 0");
        }

        Positive(nameof(pricing.DefaultPricePerKwh), pricing.DefaultPricePerKwh);
        Positive(nameof(pricing.DefaultCostPerKwh), pricing.DefaultCostPerKwh);
        Positive(nameof(pricing.DefaultMinimumRequiredBalance), pricing.DefaultMinimumRequiredBalance);
        Positive(nameof(pricing.GracePeriod), pricing.GracePeriod);
        if (pricing.Vat <= 0 || pricing.Vat >= 1)
            _result.Error(step, $"GlobalConfigurations:Vat is {pricing.Vat}, expected a rate such as 0.2");
        if (pricing.DefaultPricePerKwh > 0 && pricing.DefaultPricePerKwh < pricing.DefaultCostPerKwh)
            _result.Warn(step, $"the default price per kWh ({pricing.DefaultPricePerKwh}) is below the cost per kWh ({pricing.DefaultCostPerKwh})");
        foreach (var (name, value) in new[]
                 {
                     (nameof(pricing.DefaultFlatFee), pricing.DefaultFlatFee), (nameof(pricing.DefaultPricePerMinute), pricing.DefaultPricePerMinute),
                     (nameof(pricing.DefaultIdleTimePricing), pricing.DefaultIdleTimePricing), (nameof(pricing.DefaultLowBalanceThresholdBuffer), pricing.DefaultLowBalanceThresholdBuffer),
                     (nameof(pricing.GraceAmount), pricing.GraceAmount),
                 })
            if (value < 0) _result.Error(step, $"GlobalConfigurations:{name} is {value}, expected 0 or more");

        if (!_result.Lines.Any(l => l.Step == step))
            _result.Ok(step, $"{pricing.DefaultPricePerKwh} MAD/kWh, VAT {pricing.Vat:P0}, minimum balance {pricing.DefaultMinimumRequiredBalance} MAD");
        return Task.CompletedTask;
    }

    private async Task EnsureTestUsers(string step)
    {
        var settings = _options.TestUsers;
        var roles = await db.Roles.Where(r => new[] { AdminRole, PartnerRole, CustomerRole }.Contains(r.Name)).ToDictionaryAsync(r => r.Name, r => r.ID);
        var partner = await EnsureTestPartner(settings.PartnerEmail);

        await EnsureTestUser(step, settings.AdminEmail, AdminRole, roles[AdminRole], null, settings.AdminPassword);
        await EnsureTestUser(step, settings.PartnerEmail, PartnerRole, roles[PartnerRole], partner.ID, settings.PartnerPassword);
        await EnsureTestUser(step, settings.CustomerEmail, CustomerRole, roles[CustomerRole], null, settings.CustomerPassword);
    }

    private async Task<Partner> EnsureTestPartner(string email)
    {
        var partner = await db.Partners.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.PartnerIdentificationNumber == TestPartnerNumber);
        var now = DateTime.UtcNow;
        if (partner == null)
        {
            partner = new Partner
            {
                Name = "Eveon Test Partner",
                Description = "Partner used by the partner@ test account. Created by the first setup job.",
                Type = PartnerTypeEnum.Other,
                Email = email,
                Phone = "",
                Country = "Morocco",
                PartnerIdentificationNumber = TestPartnerNumber,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();
        }
        else if (partner.IsDeleted)
        {
            partner.IsDeleted = false;
            partner.UpdatedAt = now;
            await db.SaveChangesAsync();
        }
        return partner;
    }

    private async Task EnsureTestUser(string step, string email, string roleName, int roleId, int? partnerId, string? fixedPassword)
    {
        email = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
        if (user is { DeletionRequestedAt: not null } or { DeletedAt: not null })
        {
            _result.Warn(step, $"{email} has a GDPR deletion pending or done, left untouched");
            return;
        }

        var created = user == null;
        if (user == null)
        {
            user = new User { FirstName = "Test", LastName = roleName, Email = email, CreatedAt = now };
            db.Users.Add(user);
        }
        else if (user.RoleID != roleId || user.PartnerID != partnerId || user.IsDeleted || user.SuspendedAt != null)
            _result.Warn(step, $"{email} reset to role {roleName}, active and not suspended");

        user.RoleID = roleId;
        user.PartnerID = partnerId;
        user.IsDeleted = false;
        user.SuspendedAt = null;
        user.SuspensionReason = null;
        user.IsEmailVerified = true;
        user.UpdatedAt = now;

        var hasFixedPassword = !string.IsNullOrEmpty(fixedPassword);
        string? password = null;
        if (hasFixedPassword
                ? !AuthHelper.VerifyPasswordHash(fixedPassword!, user.PasswordHash, user.PasswordSalt)
                : created || _options.TestUsers.RotatePasswordsOnStartup || !user.HasPassword)
        {
            password = hasFixedPassword ? fixedPassword! : GeneratePassword();
            AuthHelper.CreatePasswordHash(password, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.ResetPasswordToken = null;
            user.ResetPasswordCode = null;
        }
        await db.SaveChangesAsync();

        if (password != null && !created)
        {
            // The old password is gone: sessions opened with it end too.
            await db.RefreshTokens.Where(t => t.UserID == user.ID && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.RevokedReason, "Test account password rotated on deploy"));
        }

        if (roleName == AdminRole && !await db.Administrators.IgnoreQueryFilters().AnyAsync(a => a.UserID == user.ID && !a.IsDeleted))
        {
            db.Administrators.Add(new Administrator { UserID = user.ID, CreatedAt = now, UpdatedAt = now });
            await db.SaveChangesAsync();
        }

        if (roleName == CustomerRole && !await db.Cards.IgnoreQueryFilters().AnyAsync(c => c.UserID == user.ID))
            await cards.CreateCardForUser(user);

        _result.Ok(step, $"{email} ({roleName}) {(created ? "created" : "ready")}" +
            (hasFixedPassword ? ", fixed password from configuration" : password != null ? ", new password" : ""));
        // Fixed passwords are already known: only generated ones are mailed.
        if (password != null && !hasFixedPassword)
            _result.Credentials.Add(new TestCredential(roleName, email, password));
    }

    // At least one upper, lower, digit and symbol, so it passes any password policy.
    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz", digits = "23456789", symbols = "!@#$%*-_?";
        var chars = (RandomNumberGenerator.GetString(upper, 1) + RandomNumberGenerator.GetString(lower, 1) +
                     RandomNumberGenerator.GetString(digits, 1) + RandomNumberGenerator.GetString(symbols, 1) +
                     RandomNumberGenerator.GetString(upper + lower + digits + symbols, 16)).ToCharArray();
        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}

public sealed record TestCredential(string Role, string Email, string Password);

public sealed class FirstSetupResult
{
    public sealed record Line(string Step, LogLevel Level, string Message);

    public List<Line> Lines { get; } = new();
    public List<TestCredential> Credentials { get; } = new();
    public bool HasErrors => Lines.Any(l => l.Level == LogLevel.Error);

    public void Ok(string step, string message) => Lines.Add(new Line(step, LogLevel.Information, message));
    public void Warn(string step, string message) => Lines.Add(new Line(step, LogLevel.Warning, message));
    public void Error(string step, string message) => Lines.Add(new Line(step, LogLevel.Error, message));
}
