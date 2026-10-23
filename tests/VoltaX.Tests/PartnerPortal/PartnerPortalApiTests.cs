using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.PartnerPortal;

/// <summary>The partner portal API: a partner sees and prices only their own stations.</summary>
public class PartnerPortalApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _partnerA = null!, _partnerB = null!, _customer = null!;
    private int _stationA, _chargePointA, _connectorA, _chargePointB, _connectorB;

    public async Task InitializeAsync()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        await _api.Seed(async db =>
        {
            var a = new Partner { Name = "Atlas Energy", Description = "", Email = "a@atlas.ma", Phone = "0600000001", PartnerIdentificationNumber = "P-A" };
            var b = new Partner { Name = "Borne Plus", Description = "", Email = "b@borne.ma", Phone = "0600000002", PartnerIdentificationNumber = "P-B" };
            db.Partners.AddRange(a, b);
            await db.SaveChangesAsync();
            _partnerA = await Seeds.User(db, "partner.a@test.ma", role: "Partner", partnerId: a.ID);
            _partnerB = await Seeds.User(db, "partner.b@test.ma", role: "Partner", partnerId: b.ID);
            _customer = await Seeds.User(db, "driver@test.ma");

            var stationA = await Seeds.Station(db, a.ID, "Atlas Agdal");
            var stationB = await Seeds.Station(db, b.ID, "Borne Maarif");
            var cpA = await Seeds.ChargePoint(db, "CP-A1", c => { c.PricePerKWh = 3m; c.Power = 22; }, stationA.ID);
            var cpB = await Seeds.ChargePoint(db, "CP-B1", c => c.PricePerKWh = 4m, stationB.ID);
            _stationA = stationA.ID;
            _chargePointA = cpA.ID;
            _chargePointB = cpB.ID;
            _connectorA = (await db.Connectors.SingleAsync(c => c.ChargePointID == cpA.ID)).ID;
            _connectorB = (await db.Connectors.SingleAsync(c => c.ChargePointID == cpB.ID)).ID;

            var card = Seeds.Card("DRV-1", 500m, _customer.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            var day = Seeds.Now.AddDays(-1);
            db.ChargingSessions.AddRange(
                Billed(_connectorA, card.ID, day, 40m, 10),
                Billed(_connectorA, card.ID, day.AddHours(3), 20m, 5),
                Billed(_connectorB, card.ID, day, 99m, 20));
        });
    }

    private ChargingSession Billed(int connectorId, int cardId, DateTime start, decimal total, double kwh) => new()
    {
        ConnectorID = connectorId, CardID = cardId, UserID = _customer.ID, StartDate = start, EndDate = start.AddMinutes(30),
        ChargedKwhs = kwh, ChargedMinutes = 30, EnergyAmount = total, TimeAmount = 0, FlatFeeAmount = 0, IdleAmount = 0, TotalAmount = total,
        VatRate = 0.2m, ChargingSessionStatus = ChargingSessionStatusEnum.Completed
    };

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Only_partner_accounts_reach_the_portal()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.Client().GetAsync("/api/partner-portal/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.Client(_customer).GetAsync("/api/partner-portal/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _api.Client(_partnerA).GetAsync("/api/partner-portal/overview")).StatusCode);
    }

    [Fact]
    public async Task The_overview_counts_only_the_partner_s_own_stations_and_revenue()
    {
        var overview = await _api.Client(_partnerA).GetFromJsonAsync<JsonElement>("/api/partner-portal/overview");

        Assert.Equal(1, overview.GetProperty("stations").GetInt32());
        Assert.Equal(1, overview.GetProperty("chargePoints").GetInt32());
        Assert.Equal(60m, overview.GetProperty("revenueTotal").GetDecimal());
    }

    [Fact]
    public async Task Stations_charge_points_and_sessions_are_scoped_to_the_partner()
    {
        var client = _api.Client(_partnerA);

        var stations = await client.GetFromJsonAsync<JsonElement[]>("/api/partner-portal/stations");
        var points = await client.GetFromJsonAsync<JsonElement[]>("/api/partner-portal/charge-points");
        var sessions = await client.GetFromJsonAsync<JsonElement>("/api/partner-portal/sessions");

        Assert.Equal("Atlas Agdal", Assert.Single(stations!).GetProperty("name").GetString());
        Assert.Equal("CP-A1", Assert.Single(points!).GetProperty("chargePointId").GetString());
        Assert.Equal(2, sessions.GetProperty("total").GetInt32());
        Assert.DoesNotContain("driver@test.ma", sessions.ToString()); // no driver personal data
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/partner-portal/charge-points/{_chargePointB}")).StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<JsonElement>($"/api/partner-portal/sessions?stationId={_stationA}")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task A_partner_edits_the_prices_of_their_own_connectors_only()
    {
        var client = _api.Client(_partnerA);
        var prices = new { pricePerKWh = 3.25m, pricePerMinute = 0.05m, pricePerIdleMinute = 0.5m, flatFee = 1.5m };

        var own = await client.PutAsJsonAsync($"/api/partner-portal/connectors/{_connectorA}/pricing", prices);
        var foreign = await client.PutAsJsonAsync($"/api/partner-portal/connectors/{_connectorB}/pricing", prices);
        var invalid = await client.PutAsJsonAsync($"/api/partner-portal/connectors/{_connectorA}/pricing", new { pricePerKWh = -1m, pricePerMinute = 0m, pricePerIdleMinute = 0m, flatFee = 0m });

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(3.25m, (await own.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("pricePerKWh").GetDecimal());
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var connectorA = await _api.Db(db => db.Connectors.SingleAsync(c => c.ID == _connectorA));
        var connectorB = await _api.Db(db => db.Connectors.SingleAsync(c => c.ID == _connectorB));
        Assert.Equal(1.5m, connectorA.FlatFee);
        Assert.Equal(4m, connectorB.PricePerKWh);
        Assert.True(await _api.Db(db => db.AuditLogs.AnyAsync(l => l.Action == "PartnerPortal.UpdateConnectorPricing" && l.EntityID == _connectorA.ToString())));
    }

    [Fact]
    public async Task The_generic_pricing_endpoint_stays_closed_to_partners()
    {
        var response = await _api.Client(_partnerA).PostAsJsonAsync($"/api/Connector/UpdateConnectorPricing/{_connectorB}", new { pricePerKWh = 0.01m, pricePerIdleMinute = 0m, costPerKwh = 0m });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Statistics_and_statements_cover_the_partner_s_sessions()
    {
        var client = _api.Client(_partnerA);

        var stats = await client.GetFromJsonAsync<JsonElement>("/api/partner-portal/statistics");
        var csv = Encoding.UTF8.GetString(await client.GetByteArrayAsync("/api/partner-portal/statement?format=csv")).TrimStart('﻿');
        var pdf = await client.GetByteArrayAsync("/api/partner-portal/statement?format=pdf&from=2026-01-01&to=2026-12-31");

        Assert.Equal(30, stats.GetProperty("daily").GetArrayLength());
        Assert.Equal(12, stats.GetProperty("monthly").GetArrayLength());
        Assert.Equal(60m, stats.GetProperty("monthly").EnumerateArray().Sum(p => p.GetProperty("revenue").GetDecimal()));
        Assert.Equal(3, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.DoesNotContain("99.00", csv);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task A_partner_replaces_their_logo_with_a_real_image_only()
    {
        var client = _api.Client(_partnerA);
        // 1x1 transparent PNG.
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        var ok = await client.PostAsync("/api/partner-portal/profile/logo", Upload(png, "logo.png", "image/png"));
        var fake = await client.PostAsync("/api/partner-portal/profile/logo", Upload(Encoding.UTF8.GetBytes("<script>alert(1)</script>"), "logo.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.StartsWith("PartnersLogos/", (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("logoUrl").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, fake.StatusCode);
    }

    private static MultipartFormDataContent Upload(byte[] bytes, string name, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", name } };
    }
}
