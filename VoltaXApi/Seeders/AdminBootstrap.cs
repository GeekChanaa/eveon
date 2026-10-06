using Microsoft.EntityFrameworkCore;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders;

/// <summary>
/// One-time setup of an empty database: the built-in roles (Admin with every permission, Customer,
/// Partner) and a first admin account. Run with --create-admin; see docs/configuration.md.
/// Safe to run again: existing roles are kept and an existing admin is left untouched.
/// </summary>
public static class AdminBootstrap
{
    public const string Flag = "--create-admin";

    public static async Task Run(VoltaXApiDbContext db, IConfiguration configuration)
    {
        var email = configuration["Bootstrap:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new InvalidOperationException("Set Bootstrap__AdminEmail to the admin's email address.");
        if (string.IsNullOrEmpty(password) || password.Length < 12)
            throw new InvalidOperationException("Set Bootstrap__AdminPassword to a password of at least 12 characters.");

        await RoleSeeder.Seed(db);
        var adminRole = await db.Roles.SingleAsync(r => r.Name == "Admin" && !r.IsDeleted);

        var existing = await db.Users.IgnoreQueryFilters().Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
        {
            if (existing.RoleID != adminRole.ID)
                throw new InvalidOperationException($"{email} already exists with role {existing.Role?.Name}; change its role from the dashboard instead.");
            Console.WriteLine($"Admin {email} already exists, nothing changed.");
            return;
        }

        AuthHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
        var now = DateTime.UtcNow;
        var user = new User
        {
            FirstName = configuration["Bootstrap:AdminFirstName"] ?? "Admin",
            LastName = configuration["Bootstrap:AdminLastName"] ?? "Eveon",
            Email = email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            AuthProvider = AuthProviderEnum.Local,
            IsEmailVerified = true,
            RoleID = adminRole.ID,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Users.Add(user);
        db.Administrators.Add(new Administrator { User = user, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        Console.WriteLine($"Created admin {email} (user {user.ID}).");
    }
}
