using System.Net;
using System.Net.Http.Json;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Services;

namespace VoltaX.Tests.Security;

/// <summary>Security response headers, CORS origin restriction and the refresh cookie's flags.</summary>
public class HttpSecurityTests
{
    [Fact]
    public async Task Responses_carry_the_security_headers()
    {
        using var api = new ApiFactory();
        var response = await api.Client().GetAsync("/health/live");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("strict-origin", response.Headers.GetValues("Referrer-Policy").Single());
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("default-src 'none'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
    }

    [Fact]
    public async Task CORS_allows_only_configured_origins()
    {
        using var api = new ApiFactory(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://app.eveon.ma" });

        async Task<HttpResponseMessage> Preflight(string origin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/Login");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            return await api.Client().SendAsync(request);
        }

        var allowed = await Preflight("https://app.eveon.ma");
        var evil = await Preflight("https://evil.example");

        Assert.Equal("https://app.eveon.ma", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(evil.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task The_refresh_cookie_is_httponly_secure_samesite_and_scoped_to_auth()
    {
        using var api = new ApiFactory();
        await api.Seed(db =>
        {
            VoltaXApi.Services.AuthService.CreatePasswordHashStatic("Str0ng-Pass!", out var hash, out var salt);
            db.Users.Add(new VoltaXApi.Models.User { Email = "cookie@test.ma", FirstName = "C", LastName = "T",
                RoleID = Seeds.Role(db, "Customer").Result.ID, PasswordHash = hash, PasswordSalt = salt });
            return Task.CompletedTask;
        });

        var login = await api.Client().PostAsJsonAsync("/api/Auth/Login", new { email = "cookie@test.ma", password = "Str0ng-Pass!" });

        Assert.True(login.IsSuccessStatusCode, login.StatusCode + ": " + await login.Content.ReadAsStringAsync());
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(RefreshTokenCookie.Name + "="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
