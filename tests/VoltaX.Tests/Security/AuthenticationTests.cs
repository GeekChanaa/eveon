using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Security;

/// <summary>
/// Token validation (signature, algorithm, issuer, audience, expiry), that a still-valid JWT stops working the
/// moment the account is suspended, and that repeated bad logins are stopped.
/// </summary>
public class AuthenticationTests
{
    private const string Protected = "/api/my/overview";

    private static string Token(string key, string? issuer = JwtService.DefaultIssuer, string? audience = JwtService.DefaultAudience,
        DateTime? expires = null, string alg = SecurityAlgorithms.HmacSha512Signature, int userId = 1)
    {
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), alg);
        var exp = expires ?? DateTime.UtcNow.AddMinutes(30);
        var token = new JwtSecurityTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Email, "x@test.ma") }),
            NotBefore = exp.AddHours(-1),
            Expires = exp,
            SigningCredentials = creds,
            Issuer = issuer,
            Audience = audience
        });
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string AlgNoneToken(int userId = 1)
    {
        string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = B64("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var payload = B64($"{{\"nameid\":\"{userId}\",\"iss\":\"{JwtService.DefaultIssuer}\",\"aud\":\"{JwtService.DefaultAudience}\",\"exp\":{exp}}}");
        return $"{header}.{payload}.";
    }

    private static async Task<(ApiFactory Api, User User)> Host()
    {
        var api = new ApiFactory();
        User user = null!;
        await api.Seed(async db =>
        {
            user = await Seeds.User(db, "me@test.ma");
            db.Cards.Add(Seeds.Card("AUTH-1", 10m, user.ID));
        });
        return (api, user);
    }

    private static HttpClient WithToken(ApiFactory api, string token)
    {
        var client = api.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task A_token_signed_with_the_right_key_is_accepted()
    {
        var (api, user) = await Host();
        using var _ = api;
        var ok = await WithToken(api, Token(ApiFactory.TokenKey, userId: user.ID)).GetAsync(Protected);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task Forged_tokens_are_refused()
    {
        var (api, user) = await Host();
        using var _ = api;
        var otherKey = new string('z', 72);

        async Task<HttpStatusCode> Status(string token) => (await WithToken(api, token).GetAsync(Protected)).StatusCode;

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(Token(otherKey, userId: user.ID)));                                   // wrong signing key
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(AlgNoneToken(user.ID)));                                             // alg none
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(Token(ApiFactory.TokenKey, issuer: "evil", userId: user.ID)));       // wrong issuer
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(Token(ApiFactory.TokenKey, audience: "evil", userId: user.ID)));     // wrong audience
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(Token(ApiFactory.TokenKey, expires: DateTime.UtcNow.AddHours(-2), userId: user.ID))); // expired
    }

    [Fact]
    public async Task A_valid_token_stops_working_the_moment_the_account_is_suspended()
    {
        var (api, user) = await Host();
        using var _ = api;
        var client = WithToken(api, Token(ApiFactory.TokenKey, userId: user.ID));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Protected)).StatusCode);

        await api.Db(async db =>
        {
            await db.Users.Where(u => u.ID == user.ID).ExecuteUpdateAsync(s => s.SetProperty(u => u.SuspendedAt, DateTime.UtcNow.AddYears(1)));
            return 0;
        });

        // Same (still cryptographically valid) token, but access is read from the database on every request.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Protected)).StatusCode);
    }

    [Fact]
    public async Task Repeated_bad_logins_are_locked_out()
    {
        // Raise the per-minute rate limit so the account-lockout control (5 failures) is what answers 429, not the limiter.
        using var api = new ApiFactory(new Dictionary<string, string?> { ["RateLimiting:AuthStrictPerMinute"] = "1000" });
        await api.Seed(db =>
        {
            AuthService.CreatePasswordHashStatic("Correct-horse-9", out var hash, out var salt);
            db.Users.Add(new User { Email = "lock@test.ma", FirstName = "L", LastName = "O", RoleID = Seeds.Role(db, "Customer").Result.ID, PasswordHash = hash, PasswordSalt = salt });
            return Task.CompletedTask;
        });
        var client = api.Client();

        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            var r = await client.PostAsJsonAsync("/api/Auth/Login", new { email = "lock@test.ma", password = "wrong-password" });
            codes.Add(r.StatusCode);
        }

        Assert.All(codes.Take(4), c => Assert.Equal(HttpStatusCode.Unauthorized, c));
        Assert.Equal(HttpStatusCode.TooManyRequests, codes[^1]); // locked out
        // Even the correct password is refused while the lockout holds.
        var afterLock = await client.PostAsJsonAsync("/api/Auth/Login", new { email = "lock@test.ma", password = "Correct-horse-9" });
        Assert.Equal(HttpStatusCode.TooManyRequests, afterLock.StatusCode);
    }

    [Fact]
    public async Task Logout_clears_the_refresh_cookie()
    {
        using var api = new ApiFactory();
        var client = api.Client();
        var response = await client.PostAsJsonAsync("/api/Auth/Logout", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            Assert.All(cookies.Where(c => c.StartsWith(RefreshTokenCookie.Name)), c => Assert.Contains("expires=", c, StringComparison.OrdinalIgnoreCase));
    }
}
