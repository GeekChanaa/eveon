using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Ocpi;
using VoltaXApi.Ocpi.Controllers;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Services;
using VoltaXApi.OCPP.Messages;

namespace VoltaX.Tests.Ocpi;

/// <summary>The OCPI transport: response envelope, pagination headers, tokens, version details and helpers.</summary>
public class OcpiProtocolTests
{
    private static JsonElement Body(IActionResult result)
    {
        var json = (JsonResult)result;
        return JsonDocument.Parse(JsonSerializer.Serialize(json.Value, json.Value!.GetType(), OcpiJson.Options)).RootElement;
    }

    [Fact]
    public void A_response_is_wrapped_in_the_OCPI_envelope()
    {
        var body = Body(OcpiControllerBase.Envelope(new List<int> { 1 }, OcpiStatusCodes.Success, null, 200));

        foreach (var field in new[] { "data", "status_code", "status_message", "timestamp" })
            Assert.True(body.TryGetProperty(field, out _), field);
        Assert.Equal(1000, body.GetProperty("status_code").GetInt32());
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$", body.GetProperty("timestamp").GetString());
    }

    [Fact]
    public void An_error_envelope_has_its_status_code_and_no_data()
    {
        var result = OcpiControllerBase.Envelope<object>(null, OcpiStatusCodes.UnknownLocation, "Unknown location.", 404);

        Assert.Equal(404, ((JsonResult)result).StatusCode);
        Assert.Equal(2003, Body(result).GetProperty("status_code").GetInt32());
        Assert.False(Body(result).TryGetProperty("data", out _));
    }

    [Fact]
    public void Status_codes_are_the_OCPI_2_2_1_values()
    {
        Assert.Equal(2000, OcpiStatusCodes.ClientError);
        Assert.Equal(2001, OcpiStatusCodes.InvalidParameters);
        Assert.Equal(2003, OcpiStatusCodes.UnknownLocation);
        Assert.Equal(2004, OcpiStatusCodes.UnknownToken);
    }

    [Fact]
    public void Pagination_sets_the_count_headers_and_links_the_next_page_with_the_filters()
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("api.eveon.ma");
        http.Request.Path = "/ocpi/cpo/2.2.1/locations";
        http.Request.QueryString = new QueryString("?date_from=2026-10-01T00:00:00Z&offset=0&limit=2");

        var page = OcpiPagination.Apply(http.Request, http.Response, 5, 0, 2);

        Assert.Equal((0, 2), (page.Offset, page.Limit));
        Assert.Equal("5", http.Response.Headers["X-Total-Count"]);
        Assert.Equal("100", http.Response.Headers["X-Limit"]);
        var link = http.Response.Headers["Link"].ToString();
        Assert.StartsWith("<https://api.eveon.ma/ocpi/cpo/2.2.1/locations?", link);
        Assert.Contains("offset=2", link);
        Assert.Contains("limit=2", link);
        Assert.Contains("date_from=", link);
        Assert.EndsWith(">; rel=\"next\"", link);
    }

    [Fact]
    public void The_last_page_has_no_link_and_bounds_are_clamped()
    {
        var last = new DefaultHttpContext();
        OcpiPagination.Apply(last.Request, last.Response, 5, 4, 2);
        Assert.False(last.Response.Headers.ContainsKey("Link"));

        Assert.Equal(new OcpiPage(0, 100), OcpiPagination.Apply(new DefaultHttpContext().Request, new DefaultHttpContext().Response, 500, -3, 1000));
    }

    [Fact]
    public void Tokens_are_random_hashed_and_read_from_both_header_formats()
    {
        var token = OcpiTokens.Generate();

        Assert.InRange(token.Length, 32, 64);                    // fits OCPI string(64)
        Assert.NotEqual(token, OcpiTokens.Generate());
        Assert.Contains(token, OcpiTokens.FromHeader(OcpiTokens.ToHeader(token))); // base64 (2.2.1)
        Assert.Contains(token, OcpiTokens.FromHeader("Token " + token));          // raw (before 2.2)
        Assert.Empty(OcpiTokens.FromHeader("Bearer " + token));
        Assert.Empty(OcpiTokens.FromHeader(null));
        Assert.Equal(64, OcpiTokens.Hash(token).Length);
        Assert.Equal(OcpiTokens.Hash(token), OcpiTokens.Hash(token));
    }

    [Fact]
    public void Version_details_list_the_module_endpoints_under_the_base_url()
    {
        var details = JsonDocument.Parse(OcpiJson.Serialize(OcpiVersionsController.BuildDetails(new OcpiOptions { BaseUrl = "https://api.eveon.ma/" }))).RootElement;

        Assert.Equal("2.2.1", details.GetProperty("version").GetString());
        var endpoints = details.GetProperty("endpoints").EnumerateArray().ToList();
        Assert.All(endpoints, e =>
        {
            Assert.True(e.TryGetProperty("identifier", out _) && e.TryGetProperty("url", out _));
            Assert.Contains(e.GetProperty("role").GetString(), new[] { "SENDER", "RECEIVER" });
        });
        Assert.Contains(endpoints, e => e.GetProperty("url").GetString() == "https://api.eveon.ma/ocpi/cpo/2.2.1/locations");
    }

    [Fact]
    public void The_push_outbox_backs_off_exponentially_up_to_an_hour()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), OcpiPushWorker.Backoff(1));
        Assert.Equal(TimeSpan.FromMinutes(2), OcpiPushWorker.Backoff(3));
        Assert.Equal(TimeSpan.FromHours(1), OcpiPushWorker.Backoff(20));
    }

    [Fact]
    public void Session_energy_is_read_from_Wh_or_kWh_register_samples()
    {
        var at = new DateTime(2026, 10, 1, 8, 30, 0, DateTimeKind.Utc);
        var meter = new List<MeterValueType>
        {
            new() { Timestamp = at, SampledValue = new() { new() { Value = 12500, Measurand = MeasurandEnumType.Energy_Active_Import_Register, UnitOfMeasure = new() { Unit = "Wh" } } } },
            new() { Timestamp = at.AddMinutes(1), SampledValue = new() { new() { Value = 13.0, Measurand = MeasurandEnumType.Energy_Active_Import_Register, UnitOfMeasure = new() { Unit = "kWh" } } } }
        };

        Assert.Equal(13.0, OcpiTransactionObserver.EnergyKwh(meter));
    }

    [Fact]
    public void OCPI_token_types_map_to_OCPP_id_token_types()
    {
        Assert.Equal(IdTokenEnumType.ISO14443, OcppOcpiCommandExecutor.IdTokenType("RFID"));
        Assert.Equal(IdTokenEnumType.Central, OcppOcpiCommandExecutor.IdTokenType("APP_USER"));
    }
}
