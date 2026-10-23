using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.Ocpi.Services;

namespace VoltaX.Tests.Ocpi;

/// <summary>
/// Roaming partners call /ocpi with their own OCPI token, never a user JWT. Over HTTP against the running API: the
/// module is invisible while disabled, a missing or non-OCPI credential gets the OCPI 401 envelope, and the
/// one-time token A only opens the registration handshake.
/// </summary>
public class OcpiAuthenticationTests : IAsyncLifetime
{
    private const string Locations = "/ocpi/cpo/2.2.1/locations";
    private const string Versions = "/ocpi/versions";

    private readonly string _tokenC = OcpiTokens.Generate();
    private readonly string _tokenA = OcpiTokens.Generate();
    private readonly ApiFactory _api = new(new Dictionary<string, string?> { ["Ocpi:Enabled"] = "true", ["Ocpi:BaseUrl"] = "http://localhost" });

    public Task InitializeAsync() => _api.Seed(db =>
    {
        db.OcpiParties.AddRange(
            new OcpiParty { Name = "Registered eMSP", CountryCode = "NL", PartyId = "EMS", Status = OcpiPartyStatus.Registered, IncomingTokenHash = OcpiTokens.Hash(_tokenC) },
            new OcpiParty { Name = "Pending eMSP", Status = OcpiPartyStatus.Pending, TokenAHash = OcpiTokens.Hash(_tokenA) });
        return Task.CompletedTask;
    });

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private Task<HttpResponseMessage> Get(string path, string? authorization)
    {
        var client = _api.Client();
        if (authorization != null)
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
        return client.GetAsync(path);
    }

    [Fact]
    public async Task Every_OCPI_endpoint_answers_404_while_the_module_is_disabled()
    {
        using var disabled = new ApiFactory();
        Assert.Equal(HttpStatusCode.NotFound, (await disabled.Client().GetAsync(Versions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await disabled.Client().GetAsync(Locations)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer abc")]
    public async Task A_missing_or_non_OCPI_credential_gets_the_401_envelope(string? authorization)
    {
        var response = await Get(Locations, authorization);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2000, body.GetProperty("status_code").GetInt32());
    }

    [Fact]
    public async Task A_registered_partner_reads_the_modules_with_its_token()
    {
        var response = await Get(Locations, OcpiTokens.ToHeader(_tokenC));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1000, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status_code").GetInt32());
    }

    [Fact]
    public async Task Token_A_opens_the_handshake_only()
    {
        Assert.Equal(HttpStatusCode.OK, (await Get(Versions, OcpiTokens.ToHeader(_tokenA))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(Locations, OcpiTokens.ToHeader(_tokenA))).StatusCode);
    }
}
