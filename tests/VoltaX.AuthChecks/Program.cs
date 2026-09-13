using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;
using VoltaXApi.Models;
using VoltaXApi.Services;
using VoltaXApi.Settings;

// Run from the repository root. All test data and changes roll back, even on failure.
var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.GetFullPath("VoltaXApi"))
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables().Build();
var options = new DbContextOptionsBuilder<VoltaXApiDbContext>()
    .UseSqlServer(configuration.GetConnectionString("DefaultConnection")).Options;
await using var db = new VoltaXApiDbContext(options);
await RefreshTokenSchema.Verify(db);
await using var transaction = await db.Database.BeginTransactionAsync();
var now = DateTime.UtcNow;
var user = new User
{
    FirstName = "Refresh", LastName = "Test", Email = $"refresh-check-{Guid.NewGuid():N}@example.invalid",
    RoleID = await db.Roles.Select(r => r.ID).FirstAsync(),
    CreatedAt = now, UpdatedAt = now
};
db.Users.Add(user);
await db.SaveChangesAsync();
var settings = new AuthTokenSettings { SlidingExpiration = false };
var jwt = new TestJwtService();
var service = new RefreshTokenService(new RefreshTokenRepository(db), db, jwt,
    new UserClaimsFactory(), Options.Create(settings), NullLogger<RefreshTokenService>.Instance);
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
async Task Rejected(Func<Task> action, string name)
{
    try { await action(); }
    catch (UnauthorizedException) { Check(true, name); return; }
    throw new Exception("FAILED: " + name);
}

var issued = await service.Issue(user.ID, "127.0.0.1", new string('a', 300));
var stored = await db.RefreshTokens.SingleAsync(t => t.UserID == user.ID);
Check(stored.TokenHash != issued.Token && stored.TokenHash.Length == 64, "stores only the hash");
Check(stored.UserAgent?.Length == 256, "bounds metadata length");
var rotated = await service.Rotate(issued.Token, null, null);
Check(rotated.RefreshToken != issued.Token && stored.RevokedAt != null, "rotation spends the predecessor");
Check(Math.Abs((rotated.RefreshTokenExpiresAt!.Value - issued.ExpiresAt).TotalMilliseconds) < 1, "fixed expiry is retained");
Check(jwt.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.ID.ToString()), "refresh rebuilds user claims");
await Rejected(() => service.Rotate(issued.Token, null, null), "replay is rejected");
await Rejected(() => service.Rotate(rotated.RefreshToken, null, null), "replay revokes the successor");
await Rejected(() => service.Rotate("unknown", null, null), "unknown token rejected");
await Rejected(() => service.Rotate("", null, null), "empty token rejected");

issued = await service.Issue(user.ID, null, null);
await service.Revoke(issued.Token);
await service.Revoke(issued.Token);
await Rejected(() => service.Rotate(issued.Token, null, null), "logout is idempotent and prevents refresh");

issued = await service.Issue(user.ID, null, null);
stored = await db.RefreshTokens.SingleAsync(t => t.UserID == user.ID && t.RevokedAt == null);
stored.ExpiresAt = now.AddMinutes(-1);
await db.SaveChangesAsync();
await Rejected(() => service.Rotate(issued.Token, null, null), "expired token rejected");

settings.SlidingExpiration = true;
issued = await service.Issue(user.ID, null, null);
stored = await db.RefreshTokens.SingleAsync(t => t.UserID == user.ID && t.RevokedAt == null && t.ExpiresAt > now);
stored.ExpiresAt = now.AddHours(1);
await db.SaveChangesAsync();
rotated = await service.Rotate(issued.Token, null, null);
Check(rotated.RefreshTokenExpiresAt > now.AddDays(1), "sliding expiry extends the session");
user.SuspendedAt = now;
await db.SaveChangesAsync();
await Rejected(() => service.Rotate(rotated.RefreshToken, null, null), "suspended account cannot refresh");
user.SuspendedAt = null;
await db.SaveChangesAsync();

issued = await service.Issue(user.ID, null, null);
stored = await db.RefreshTokens.SingleAsync(t => t.UserID == user.ID && t.RevokedAt == null && t.ExpiresAt > now);
// Simulate a second connection spending the row after this context read it.
await db.RefreshTokens.Where(t => t.ID == stored.ID)
    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
await Rejected(() => service.Rotate(issued.Token, null, null), "stale concurrent rotation is rejected");
Check(!await db.RefreshTokens.AnyAsync(t => t.UserID == user.ID && t.RevokedAt == null && t.ExpiresAt > now),
    "concurrent rotation does not leave a usable successor");
await transaction.RollbackAsync();
Console.WriteLine($"{checks} SQL Server authentication checks passed; test changes rolled back.");

sealed class TestJwtService : IJwtService
{
    public List<Claim> Claims { get; private set; } = new();
    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(60);
    public string GenerateToken(List<Claim> claims) => GenerateAccessToken(claims).Token;
    public AccessToken GenerateAccessToken(List<Claim> claims)
    {
        Claims = claims;
        return new AccessToken("test-access", DateTime.UtcNow.Add(AccessTokenLifetime));
    }
}
