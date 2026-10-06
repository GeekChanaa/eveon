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
// The mobile controllers run against the same real SQL schema inside this rollback transaction.
var mobile = new VoltaXApi.Controllers.MobileController(db) {
    ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext {
        HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
    }
};
Check(await mobile.Wallet() is Microsoft.AspNetCore.Mvc.UnauthorizedResult, "anonymous wallet rejected");
Check(await mobile.Orders() is Microsoft.AspNetCore.Mvc.UnauthorizedResult, "anonymous orders rejected");
Check(await mobile.Reviews(1, 0) is Microsoft.AspNetCore.Mvc.BadRequestResult, "invalid review pagination rejected");
mobile.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()) }, "test"));
var ownCard = new Card { UserID = user.ID, CardNumber = "mobile-check-1234", Note = "test", Balance = 93.4, ExpirationDate = now.AddYears(1) };
var otherCard = new Card { CardNumber = "mobile-check-5678", Note = "test", Balance = 999, ExpirationDate = now.AddYears(1) };
db.Cards.AddRange(ownCard, otherCard);
await db.SaveChangesAsync();
for (var i = 0; i < 11; i++) db.Orders.Add(new Order { CardID = ownCard.ID, Amount = 10, Status = i == 0 ? RechargeOrderStatus.Pending : RechargeOrderStatus.Completed, RechargeDate = now.AddSeconds(-i) });
db.Orders.Add(new Order { CardID = otherCard.ID, Amount = 999, Status = RechargeOrderStatus.Completed, RechargeDate = now });
await db.SaveChangesAsync();
System.Text.Json.JsonElement Payload(Microsoft.AspNetCore.Mvc.IActionResult result) =>
    System.Text.Json.JsonSerializer.SerializeToElement(((Microsoft.AspNetCore.Mvc.OkObjectResult)result).Value,
        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
var wallet = Payload(await mobile.Wallet());
Check(Math.Abs(wallet.GetProperty("balance").GetDouble() - 93.4) < .001, "wallet excludes other owners");
Check(wallet.GetProperty("cards")[0].GetProperty("last4").GetString() == "1234", "wallet exposes masked card suffix");
Check(!wallet.GetRawText().Contains("mobile-check-1234"), "wallet omits complete card identifiers");
Check(wallet.GetProperty("monthlyTopUps").GetDouble() == 100, "insights exclude pending and other owners");
var firstPage = Payload(await mobile.Orders());
var lastPage = Payload(await mobile.Orders(2));
Check(firstPage.GetProperty("items").GetArrayLength() == 10 && firstPage.GetProperty("hasMore").GetBoolean(), "orders paginate after ten");
Check(lastPage.GetProperty("items").GetArrayLength() == 1 && !lastPage.GetProperty("hasMore").GetBoolean(), "orders final page has one item");
Check(Payload(await mobile.Orders(status: "Pending")).GetProperty("total").GetInt32() == 1, "order status filter uses backend state");
Check(await mobile.Orders(status: "invalid") is Microsoft.AspNetCore.Mvc.BadRequestResult, "invalid order filter rejected");
foreach (var input in new[] { "0612345678", "6 12 34 56 78", "+212612345678", "00212612345678", "212612345678" })
    Check(AuthService.NormalizeMobilePhone(input) == "+212612345678", "Moroccan mobile normalization");
foreach (var input in new[] { "", "+33612345678", "0512345678", "061234567", "06123456789", "++212612345678", "+2120612345678" })
{
    var invalid = false;
    try { AuthService.NormalizeMobilePhone(input); } catch (ArgumentException) { invalid = true; }
    Check(invalid, "invalid mobile rejected");
}
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
user.SuspendedAt = now.AddHours(1);
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
