using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.CustomerPortal;

/// <summary>The customer portal API: the caller's own data only, read only (but for the optional profile details), with exports and invoices.</summary>
public class CustomerPortalApiTests : IClassFixture<CustomerPortalApiTests.Fixture>
{
    public sealed class Fixture : IAsyncLifetime
    {
        public ApiFactory Api { get; } = new();
        public User Customer { get; private set; } = null!;
        public User Other { get; private set; } = null!;
        public int BilledSessionId, LegacySessionId, ActiveSessionId, OtherSessionId, PaidOrderId, PendingOrderId;

        public async Task InitializeAsync()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            await Api.Seed(async db =>
            {
                Customer = await Seeds.User(db, "me@test.ma");
                Other = await Seeds.User(db, "you@test.ma");
                var station = await Seeds.Station(db, name: "Marina Casablanca");
                var cp = await Seeds.ChargePoint(db, "CP-MARINA", c => c.PricePerKWh = 2.5m, station.ID);
                var connector = await db.Connectors.SingleAsync(c => c.ChargePointID == cp.ID);
                var card = Seeds.Card("MY-0001", 120m, Customer.ID);
                var otherCard = Seeds.Card("YOU-0002", 50m, Other.ID);
                db.Cards.AddRange(card, otherCard);
                await db.SaveChangesAsync();

                var t0 = Seeds.Now;
                var billed = new ChargingSession
                {
                    ConnectorID = connector.ID, UserID = Customer.ID, CardID = card.ID, StartDate = t0.AddDays(-2), EndDate = t0.AddDays(-2).AddMinutes(30),
                    ChargedKwhs = 12, ChargedMinutes = 30, IdleMinutes = 0, PricePerKWh = 2.5m, PricePerMinute = 0.1m, FlatFee = 1m, VatRate = 0.2m,
                    EnergyAmount = 30m, TimeAmount = 3m, FlatFeeAmount = 1m, IdleAmount = 0m, TotalAmount = 34m, BalanceBefore = 154m, BalanceAfter = 120m,
                    ChargingSessionStatus = ChargingSessionStatusEnum.Completed, StoppedReason = VoltaXApi.OCPP.Messages.ReasonEnumType.EVDisconnected
                };
                var legacy = new ChargingSession
                {
                    ConnectorID = connector.ID, UserID = Customer.ID, CardID = card.ID, StartDate = t0.AddDays(-40), EndDate = t0.AddDays(-40).AddMinutes(60),
                    ChargedKwhs = 20, ChargedMinutes = 60, PricePerMinute = 0.5m, ChargingSessionStatus = ChargingSessionStatusEnum.Completed
                };
                var active = new ChargingSession
                {
                    ConnectorID = connector.ID, UserID = Customer.ID, CardID = card.ID, StartDate = t0, ChargedKwhs = 4, ChargedMinutes = 10,
                    PricePerKWh = 2.5m, PricePerMinute = 0m, FlatFee = 0m, VatRate = 0.2m, ChargingSessionStatus = ChargingSessionStatusEnum.Pending
                };
                var other = new ChargingSession
                {
                    ConnectorID = connector.ID, UserID = Other.ID, CardID = otherCard.ID, StartDate = t0.AddDays(-1), EndDate = t0.AddDays(-1).AddMinutes(20),
                    ChargedKwhs = 5, ChargedMinutes = 20, TotalAmount = 12.5m, EnergyAmount = 12.5m, ChargingSessionStatus = ChargingSessionStatusEnum.Completed
                };
                db.ChargingSessions.AddRange(billed, legacy, active, other);
                var paid = new Order { CardID = card.ID, Amount = 100m, Status = RechargeOrderStatus.Completed, Provider = "Sandbox", PaidAt = t0, RechargeDate = t0 };
                var pending = new Order { CardID = card.ID, Amount = 50m, Status = RechargeOrderStatus.Pending, Provider = "Sandbox", RechargeDate = t0 };
                db.Orders.AddRange(paid, pending, new Order { CardID = otherCard.ID, Amount = 10m, Status = RechargeOrderStatus.Completed, RechargeDate = t0 });
                await db.SaveChangesAsync();
                db.WalletTransactions.Add(new WalletTransaction { CardID = card.ID, Type = WalletTransactionType.SessionCharge, Amount = -34m, BalanceAfter = 120m,
                    Reference = $"session:{billed.ID}:charge", ChargingSessionID = billed.ID, CreatedAt = t0 });
                BilledSessionId = billed.ID; LegacySessionId = legacy.ID; ActiveSessionId = active.ID; OtherSessionId = other.ID;
                PaidOrderId = paid.ID; PendingOrderId = pending.ID;
            });
        }

        public Task DisposeAsync()
        {
            Api.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly Fixture _f;
    private readonly HttpClient _me;

    public CustomerPortalApiTests(Fixture fixture)
    {
        _f = fixture;
        _me = fixture.Api.Client(fixture.Customer);
    }

    [Fact]
    public async Task The_overview_sums_the_caller_s_sessions_and_wallet()
    {
        var overview = await _me.GetFromJsonAsync<JsonElement>("/api/my/overview");

        Assert.Equal(120m, overview.GetProperty("walletBalance").GetDecimal());
        Assert.Equal(3, overview.GetProperty("sessionsTotal").GetInt32());
        Assert.Equal(36, overview.GetProperty("kwhTotal").GetDouble(), 6);
        Assert.Equal(34m + 30m, overview.GetProperty("spentTotal").GetDecimal()); // billed + legacy time-based (60 x 0.5); active not billed yet
        Assert.Equal(10.00m, overview.GetProperty("activeSession").GetProperty("currentCost").GetDecimal());
    }

    [Fact]
    public async Task Sessions_list_only_the_caller_s_own_with_amounts()
    {
        var page = await _me.GetFromJsonAsync<JsonElement>("/api/my/sessions?pageSize=10");
        var items = page.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(3, page.GetProperty("total").GetInt32());
        Assert.DoesNotContain(items, i => i.GetProperty("id").GetInt32() == _f.OtherSessionId);
        var billed = items.Single(i => i.GetProperty("id").GetInt32() == _f.BilledSessionId);
        Assert.Equal(34m, billed.GetProperty("total").GetDecimal());
        Assert.Equal("Marina Casablanca", billed.GetProperty("stationName").GetString());
        var legacy = items.Single(i => i.GetProperty("id").GetInt32() == _f.LegacySessionId);
        Assert.Equal(30m, legacy.GetProperty("timeAmount").GetDecimal());
        Assert.Equal(_f.ActiveSessionId, items[0].GetProperty("id").GetInt32()); // newest first
    }

    [Theory]
    [InlineData("status=active", 1)]
    [InlineData("status=completed", 2)]
    [InlineData("search=Marina", 3)]
    [InlineData("search=Nowhere", 0)]
    [InlineData("from=2026-10-01", 2)]
    [InlineData("to=2026-09-30", 1)]
    public async Task Sessions_can_be_filtered(string query, int expected)
    {
        var page = await _me.GetFromJsonAsync<JsonElement>("/api/my/sessions?" + query);
        Assert.Equal(expected, page.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task A_session_detail_splits_the_vat_and_belongs_to_the_caller()
    {
        var detail = await _me.GetFromJsonAsync<JsonElement>($"/api/my/sessions/{_f.BilledSessionId}");
        var foreign = await _me.GetAsync($"/api/my/sessions/{_f.OtherSessionId}");

        Assert.Equal(28.33m, detail.GetProperty("totalExclVat").GetDecimal());
        Assert.Equal(5.67m, detail.GetProperty("vat").GetDecimal());
        Assert.Equal("0001", detail.GetProperty("cardLast4").GetString());
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
    }

    [Fact]
    public async Task Invoices_exist_for_the_caller_s_ended_sessions_and_paid_top_ups_only()
    {
        Assert.Equal("%PDF", await Head(await _me.GetAsync($"/api/my/sessions/{_f.BilledSessionId}/invoice")));
        Assert.Equal(HttpStatusCode.NotFound, (await _me.GetAsync($"/api/my/sessions/{_f.ActiveSessionId}/invoice")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _me.GetAsync($"/api/my/sessions/{_f.OtherSessionId}/invoice")).StatusCode);
        Assert.Equal("%PDF", await Head(await _me.GetAsync($"/api/my/orders/{_f.PaidOrderId}/invoice")));
        Assert.Equal(HttpStatusCode.NotFound, (await _me.GetAsync($"/api/my/orders/{_f.PendingOrderId}/invoice")).StatusCode);
    }

    [Fact]
    public async Task Sessions_export_to_csv_and_to_a_printable_pdf()
    {
        var csv = await _me.GetAsync("/api/my/sessions/export?format=csv&status=completed");
        var bytes = await csv.Content.ReadAsByteArrayAsync();
        var text = Encoding.UTF8.GetString(bytes);

        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        Assert.True(bytes.Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
        var lines = text.TrimStart('﻿').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("Session;Start;End;Station", lines[0]);
        Assert.Equal(3, lines.Length);
        Assert.Contains(lines, l => l.Contains(";34.00;EVDisconnected"));

        Assert.Equal("%PDF", await Head(await _me.GetAsync("/api/my/sessions/export?format=pdf&from=2026-01-01&to=2026-12-31")));
        Assert.Equal(HttpStatusCode.BadRequest, (await _me.GetAsync("/api/my/sessions/export?format=xls")).StatusCode);
    }

    [Fact]
    public async Task Wallet_movements_and_top_ups_are_the_caller_s_own()
    {
        var wallet = await _me.GetFromJsonAsync<JsonElement>("/api/my/wallet");
        var orders = await _me.GetFromJsonAsync<JsonElement>("/api/my/orders");

        Assert.Contains(wallet.GetProperty("items").EnumerateArray(), m => m.GetProperty("amount").GetDecimal() == -34m);
        Assert.Equal(2, orders.GetProperty("total").GetInt32());
        Assert.All(orders.GetProperty("items").EnumerateArray(), o => Assert.Equal("0001", o.GetProperty("cardLast4").GetString()));
    }

    [Fact]
    public async Task The_portal_is_read_only_and_requires_a_session()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.Api.Client().GetAsync("/api/my/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await _me.PostAsJsonAsync("/api/my/profile", new { firstName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await _me.DeleteAsync($"/api/my/sessions/{_f.BilledSessionId}")).StatusCode);
        var cards = await _me.GetFromJsonAsync<JsonElement>("/api/my/cards");
        Assert.DoesNotContain("MY-0001", cards.ToString()); // full card numbers never leave the API
    }

    [Fact]
    public async Task The_caller_fills_in_and_clears_their_optional_details()
    {
        var model = new ElectricVehicleModel { Make = "Renault", Model = "Zoe", Type = "", Range = "", BatteryCapacity = "", ImageSrc = "", LogoSrc = "" };
        await _f.Api.Seed(async db => { db.ElectricVehicleModels.Add(model); await db.SaveChangesAsync(); });
        var other = _f.Api.Client(_f.Other);

        var saved = await other.PutAsJsonAsync("/api/my/profile",
            new { birthday = "1990-04-12", gender = "Female", city = " Rabat ", electricVehicleModelId = model.ID });
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        var profile = await other.GetFromJsonAsync<JsonElement>("/api/my/profile");
        Assert.Equal("Female", profile.GetProperty("gender").GetString());
        Assert.Equal("Rabat", profile.GetProperty("city").GetString());
        Assert.Equal("Renault Zoe", profile.GetProperty("electricVehicleModelName").GetString());
        Assert.StartsWith("1990-04-12", profile.GetProperty("birthday").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await other.PutAsJsonAsync("/api/my/profile", new { })).StatusCode);
        profile = await other.GetFromJsonAsync<JsonElement>("/api/my/profile");
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("city").ValueKind);
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("electricVehicleModelId").ValueKind);
        // The customer's own account was not touched.
        Assert.Equal(JsonValueKind.Null, (await _me.GetFromJsonAsync<JsonElement>("/api/my/profile")).GetProperty("city").ValueKind);
    }

    [Theory]
    [InlineData("{\"gender\":\"Robot\"}")]
    [InlineData("{\"birthday\":\"2999-01-01\"}")]
    [InlineData("{\"electricVehicleModelId\":987654}")]
    public async Task Invalid_profile_details_are_refused(string body)
    {
        var response = await _me.PutAsync("/api/my/profile", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<string> Head(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return Encoding.ASCII.GetString(bytes, 0, 4);
    }
}
