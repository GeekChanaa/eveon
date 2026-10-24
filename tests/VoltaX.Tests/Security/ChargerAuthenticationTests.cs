using System.Text;
using Microsoft.AspNetCore.Http;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Pki;
using VoltaXApi.OCPP.Services;

namespace VoltaX.Tests.Security;

/// <summary>
/// Transport-level charger authentication (OCPP security profiles 1/2 Basic auth): the Basic username must be the
/// charge point identity from the URL, the password is verified against the stored PBKDF2 hash, a charger with no
/// credentials is refused unless explicitly allowed, and plain ws:// is refused outside profile 1.
/// </summary>
public class ChargerAuthenticationTests
{
    private const string Password = "a-strong-charger-password";

    private static HttpContext Context(string? authorization = null, bool https = false)
    {
        var http = new DefaultHttpContext();
        if (https) http.Request.Scheme = "https";
        if (authorization != null) http.Request.Headers["Authorization"] = authorization;
        return http;
    }

    private static string Basic(string username, string password) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

    private static AuthenticationService Service(bool allowInsecureProfile1 = true) =>
        new(new OcppTransportSettings { AllowInsecureProfile1 = allowInsecureProfile1 }, validator: null);

    private static ChargePoint Charger(string id, string? password = Password, int profile = 1) =>
        new() { ChargePointId = id, Password = password == null ? null : ChargePointPasswordHasher.Hash(password), SecurityProfile = profile };

    [Fact]
    public void Correct_username_and_password_are_accepted()
    {
        var result = Service().AuthenticateChargePoint(Context(Basic("CP1", Password)), Charger("CP1"), "CP1", out var upgraded);
        Assert.Equal(ChargePointAuthResult.Success, result);
        Assert.Null(upgraded);
    }

    [Fact]
    public void A_charger_cannot_present_another_charge_point_id_as_its_basic_username()
    {
        // Correct password, but the Basic username is a different charger's identity than the URL.
        var result = Service().AuthenticateChargePoint(Context(Basic("CP2", Password)), Charger("CP1"), "CP1", out _);
        Assert.Equal(ChargePointAuthResult.UsernameMismatch, result);
    }

    [Fact]
    public void A_wrong_password_is_refused()
    {
        var result = Service().AuthenticateChargePoint(Context(Basic("CP1", "not-the-password")), Charger("CP1"), "CP1", out _);
        Assert.Equal(ChargePointAuthResult.InvalidPassword, result);
    }

    [Fact]
    public void A_missing_authorization_header_is_refused()
    {
        var result = Service().AuthenticateChargePoint(Context(), Charger("CP1"), "CP1", out _);
        Assert.Equal(ChargePointAuthResult.MissingOrMalformedHeader, result);
    }

    [Fact]
    public void A_legacy_plaintext_password_matches_and_is_rehashed()
    {
        var charger = new ChargePoint { ChargePointId = "CP1", Password = "legacy-plaintext-pass", SecurityProfile = 1 };
        var result = Service().AuthenticateChargePoint(Context(Basic("CP1", "legacy-plaintext-pass")), charger, "CP1", out var upgraded);

        Assert.Equal(ChargePointAuthResult.Success, result);
        Assert.True(ChargePointPasswordHasher.IsHashed(upgraded)); // the caller stores the upgraded hash
    }

    [Fact]
    public void A_charger_without_credentials_is_refused()
    {
        var result = Service().AuthenticateChargePoint(Context(Basic("CP1", Password)), Charger("CP1", password: null), "CP1", out _);
        Assert.Equal(ChargePointAuthResult.NoCredentialsConfigured, result);
    }

    [Fact]
    public void Profile_1_over_plain_ws_is_refused_when_insecure_transport_is_off()
    {
        var result = Service(allowInsecureProfile1: false)
            .AuthenticateChargePoint(Context(Basic("CP1", Password), https: false), Charger("CP1"), "CP1", out _);
        Assert.Equal(ChargePointAuthResult.InsecureTransportNotAllowed, result);
    }

    [Fact]
    public void Profile_2_requires_tls()
    {
        var overPlain = Service().AuthenticateChargePoint(Context(Basic("CP1", Password), https: false), Charger("CP1", profile: 2), "CP1", out _);
        var overTls = Service().AuthenticateChargePoint(Context(Basic("CP1", Password), https: true), Charger("CP1", profile: 2), "CP1", out _);

        Assert.Equal(ChargePointAuthResult.TlsRequired, overPlain);
        Assert.Equal(ChargePointAuthResult.Success, overTls);
    }

    [Fact]
    public void Password_hashing_is_salted_constant_time_and_verifies()
    {
        var h1 = ChargePointPasswordHasher.Hash(Password);
        var h2 = ChargePointPasswordHasher.Hash(Password);

        Assert.NotEqual(h1, h2); // random salt
        Assert.StartsWith("PBKDF2-SHA256$", h1);
        Assert.True(ChargePointPasswordHasher.Verify(h1, Password, out var rehash) && !rehash);
        Assert.False(ChargePointPasswordHasher.Verify(h1, "wrong", out _));
    }
}
