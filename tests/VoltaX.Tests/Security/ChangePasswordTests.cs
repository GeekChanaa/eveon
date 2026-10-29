using System.Net;
using System.Net.Http.Json;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Security;

/// <summary>
/// POST api/Auth/ChangePassword: only the caller's own password, only with the current one, a confirmed new one that
/// differs from it, readable 400s instead of 500s, and the same rate limit as sign in.
/// </summary>
public class ChangePasswordTests
{
    private const string Current = "Current-pass-1";
    private const string Next = "Brand-new-pass-2";

    private static async Task<(ApiFactory Api, User Me, User Other)> Host(IDictionary<string, string?>? settings = null)
    {
        var api = new ApiFactory(settings ?? new Dictionary<string, string?> { ["RateLimiting:AuthStrictPerMinute"] = "1000" });
        User me = null!, other = null!;
        await api.Seed(async db =>
        {
            me = await Seeds.User(db, "me@test.ma");
            other = await Seeds.User(db, "other@test.ma");
            foreach (var user in new[] { me, other })
            {
                AuthService.CreatePasswordHashStatic(Current, out var hash, out var salt);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
            }
        });
        return (api, me, other);
    }

    private static Task<HttpResponseMessage> Change(HttpClient client, int id, string current, string next, string? check = null) =>
        client.PostAsJsonAsync("/api/Auth/ChangePassword", new { id, currentPassword = current, newPassword = next, newPasswordCheck = check ?? next });

    private static async Task<HttpStatusCode> Login(ApiFactory api, string email, string password) =>
        (await api.Client().PostAsJsonAsync("/api/Auth/Login", new { email, password })).StatusCode;

    [Fact]
    public async Task The_current_password_changes_the_password()
    {
        var (api, me, _) = await Host();
        using var _ = api;

        Assert.Equal(HttpStatusCode.Created, (await Change(api.Client(me), me.ID, Current, Next)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await Login(api, me.Email, Next));
        Assert.Equal(HttpStatusCode.Unauthorized, await Login(api, me.Email, Current));
    }

    [Fact]
    public async Task A_wrong_current_password_is_refused_and_changes_nothing()
    {
        var (api, me, _) = await Host();
        using var _ = api;

        Assert.Equal(HttpStatusCode.Forbidden, (await Change(api.Client(me), me.ID, "Not-the-pass-3", Next)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await Login(api, me.Email, Current));
    }

    [Fact]
    public async Task Another_users_password_cannot_be_changed()
    {
        var (api, me, other) = await Host();
        using var _ = api;

        // Even knowing the other account's password: the access filter only lets the caller's own ID through.
        Assert.Equal(HttpStatusCode.Forbidden, (await Change(api.Client(me), other.ID, Current, Next)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await Login(api, other.Email, Current));
    }

    [Theory]
    [InlineData(Next, "Brand-new-pass-X")] // confirmation differs
    [InlineData(Current, null)]            // same as the current one
    [InlineData("weak", null)]             // fails [StrongPassword]
    public async Task Invalid_new_passwords_are_400(string next, string? check)
    {
        var (api, me, _) = await Host();
        using var _ = api;

        Assert.Equal(HttpStatusCode.BadRequest, (await Change(api.Client(me), me.ID, Current, next, check)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await Login(api, me.Email, Current));
    }

    [Fact]
    public async Task Missing_fields_are_400_not_500()
    {
        var (api, me, _) = await Host();
        using var _ = api;

        var response = await api.Client(me).PostAsJsonAsync("/api/Auth/ChangePassword", new { id = me.ID });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Guessing_the_current_password_is_rate_limited()
    {
        var (api, me, _) = await Host(new Dictionary<string, string?> { ["RateLimiting:AuthStrictPerMinute"] = "3" });
        using var _ = api;
        var client = api.Client(me);

        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
            codes.Add((await Change(client, me.ID, $"Guess-number-{i}!", Next)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, codes[0]);
        Assert.Equal(HttpStatusCode.TooManyRequests, codes[^1]);
    }
}
