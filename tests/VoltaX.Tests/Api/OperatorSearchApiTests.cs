using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaX.Tests.Api;

/// <summary>The operator's network-wide session and transaction search, exports and session page.</summary>
public class OperatorSearchApiTests : IClassFixture<OperatorSearchApiTests.Fixture>
{
    public sealed class Fixture : IAsyncLifetime
    {
        public ApiFactory Api { get; } = new();
        public User Admin { get; private set; } = null!;
        public User Customer { get; private set; } = null!;
        public int BilledId, LegacyId, ActiveId, DeletedId, ChargePoint2Id;

        public async Task InitializeAsync()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            await Api.Seed(async db =>
            {
                Admin = await Seeds.User(db, "ops@test.ma", role: "Admin");
                Customer = await Seeds.User(db, "ali@test.ma");
                var marina = await Seeds.Station(db, name: "Marina");
                var agdal = await Seeds.Station(db, name: "Agdal");
                var cp1 = await Seeds.ChargePoint(db, "CP-OPS-1", c => c.PricePerKWh = 2.5m, marina.ID);
                var cp2 = await Seeds.ChargePoint(db, "CP-OPS-2", c => c.PricePerKWh = 3m, agdal.ID);
                ChargePoint2Id = cp2.ID;
                var c1 = await db.Connectors.SingleAsync(c => c.ChargePointID == cp1.ID);
                var c2 = await db.Connectors.SingleAsync(c => c.ChargePointID == cp2.ID);
                var card = Seeds.Card("OPS-00001234", 100m, Customer.ID);
                db.Cards.Add(card);
                await db.SaveChangesAsync();

                var t0 = Seeds.Now;
                var billed = new ChargingSession
                {
                    ConnectorID = c1.ID, UserID = Customer.ID, CardID = card.ID, StartDate = t0.AddDays(-2), EndDate = t0.AddDays(-2).AddMinutes(30),
                    ChargedKwhs = 12, ChargedMinutes = 30, PricePerKWh = 2.5m, PricePerMinute = 0.1m, FlatFee = 1m, VatRate = 0.2m,
                    EnergyAmount = 30m, TimeAmount = 3m, FlatFeeAmount = 1m, IdleAmount = 0m, TotalAmount = 34m,
                    ChargingSessionStatus = ChargingSessionStatusEnum.Completed, StoppedReason = ReasonEnumType.EVDisconnected
                };
                var legacy = new ChargingSession
                {
                    ConnectorID = c1.ID, UserID = Customer.ID, CardID = card.ID, StartDate = t0.AddDays(-40), EndDate = t0.AddDays(-40).AddMinutes(60),
                    ChargedKwhs = 20, ChargedMinutes = 60, PricePerMinute = 0.5m, ChargingSessionStatus = ChargingSessionStatusEnum.Completed
                };
                var active = new ChargingSession
                {
                    ConnectorID = c2.ID, CardID = card.ID, StartDate = t0, ChargedKwhs = 4, ChargedMinutes = 10, PricePerKWh = 3m,
                    PricePerMinute = 0m, FlatFee = 0m, VatRate = 0.2m, ChargingSessionStatus = ChargingSessionStatusEnum.Pending
                };
                var deleted = new ChargingSession
                {
                    ConnectorID = c1.ID, CardID = card.ID, StartDate = t0.AddDays(-1), EndDate = t0.AddDays(-1).AddMinutes(5),
                    IsDeleted = true, ChargingSessionStatus = ChargingSessionStatusEnum.Completed
                };
                db.ChargingSessions.AddRange(billed, legacy, active, deleted);
                await db.SaveChangesAsync();
                db.Transactions.AddRange(
                    new Transaction { Uid = "TX-1", ChargingSessionID = billed.ID, ConnectorID = c1.ID, StartCardID = card.ID, StartTime = billed.StartDate,
                        StopTime = billed.EndDate, MeterStart = 1000, MeterStop = 13000, StopReason = "EVDisconnected", Status = TransactionStatusEnum.Ended },
                    new Transaction { Uid = "TX-2", ChargingSessionID = active.ID, ConnectorID = c2.ID, StartCardID = card.ID, StartTime = active.StartDate,
                        MeterStart = 500, Status = TransactionStatusEnum.Current });
                await db.SaveChangesAsync();
                BilledId = billed.ID; LegacyId = legacy.ID; ActiveId = active.ID; DeletedId = deleted.ID;
            });
        }

        public Task DisposeAsync()
        {
            Api.Dispose();
            return Task.CompletedTask;
        }
    }

    private readonly Fixture _f;
    private readonly HttpClient _ops;

    public OperatorSearchApiTests(Fixture fixture)
    {
        _f = fixture;
        _ops = fixture.Api.Client(fixture.Admin);
    }

    [Theory]
    [InlineData("/api/ChargingSession/Search")]
    [InlineData("/api/ChargingSession/Export?format=csv")]
    [InlineData("/api/Transaction/Search")]
    [InlineData("/api/Transaction/Export")]
    public async Task Customers_cannot_search_the_network(string url)
    {
        var response = await _f.Api.Client(_f.Customer).GetAsync(url);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Sessions_come_newest_first_with_their_amounts_and_without_deleted_ones()
    {
        var page = await _ops.GetFromJsonAsync<JsonElement>("/api/ChargingSession/Search");
        var items = page.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(3, page.GetProperty("total").GetInt32());
        Assert.Equal(new[] { _f.ActiveId, _f.BilledId, _f.LegacyId }, items.Select(i => i.GetProperty("id").GetInt32()));
        var billed = items[1];
        Assert.Equal(34m, billed.GetProperty("total").GetDecimal());
        Assert.True(billed.GetProperty("billed").GetBoolean());
        Assert.Equal("Marina", billed.GetProperty("stationName").GetString());
        Assert.Equal("CP-OPS-1", billed.GetProperty("chargePointName").GetString());
        Assert.Equal("EVSE 1", billed.GetProperty("connector").GetString());
        Assert.Equal("1234", billed.GetProperty("cardLast4").GetString());
        Assert.Equal("Ev Driver", billed.GetProperty("userName").GetString());
        Assert.Equal(30m, items[2].GetProperty("total").GetDecimal()); // legacy: 60 min x 0.5
        Assert.True(items[0].GetProperty("active").GetBoolean());
    }

    [Theory]
    [InlineData("status=active", 1)]
    [InlineData("status=completed", 2)]
    [InlineData("search=CP-OPS-2", 1)]
    [InlineData("search=Agdal", 1)]
    [InlineData("search=ali@test.ma", 2)]
    [InlineData("search=Ev Driver", 2)]
    [InlineData("search=00001234", 3)]
    [InlineData("search=Nowhere", 0)]
    [InlineData("from=2026-10-01", 2)]
    [InlineData("to=2026-09-30", 1)]
    [InlineData("from=2026-10-18&to=2026-10-18", 1)]
    public async Task Sessions_can_be_filtered(string query, int expected)
    {
        var page = await _ops.GetFromJsonAsync<JsonElement>("/api/ChargingSession/Search?" + query);
        Assert.Equal(expected, page.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Sessions_can_be_found_by_id_charge_point_and_customer()
    {
        var byId = await _ops.GetFromJsonAsync<JsonElement>($"/api/ChargingSession/Search?search=%23{_f.BilledId}");
        var byPoint = await _ops.GetFromJsonAsync<JsonElement>($"/api/ChargingSession/Search?chargePointId={_f.ChargePoint2Id}");
        var byUser = await _ops.GetFromJsonAsync<JsonElement>($"/api/ChargingSession/Search?userId={_f.Customer.ID}");

        Assert.Equal(_f.BilledId, byId.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal(1, byId.GetProperty("total").GetInt32());
        Assert.Equal(_f.ActiveId, byPoint.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal(2, byUser.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Pages_are_bounded()
    {
        var page = await _ops.GetFromJsonAsync<JsonElement>("/api/ChargingSession/Search?pageSize=5000&page=0");

        Assert.Equal(100, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, page.GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task Sessions_export_as_csv_and_pdf()
    {
        var csv = await _ops.GetAsync("/api/ChargingSession/Export?format=csv&status=completed");
        var pdf = await _ops.GetAsync("/api/ChargingSession/Export?format=pdf");
        var bad = await _ops.GetAsync("/api/ChargingSession/Export?format=xls");

        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        var lines = Encoding.UTF8.GetString(await csv.Content.ReadAsByteArrayAsync()).TrimStart('﻿').Trim().Split('\n');
        Assert.Equal(3, lines.Length); // header + 2 completed sessions, oldest first
        Assert.StartsWith($"{_f.LegacyId};", lines[1]);
        Assert.Contains(";34.00;", lines[2]);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.Equal("%PDF", Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Transactions_can_be_searched_and_exported()
    {
        var all = await _ops.GetFromJsonAsync<JsonElement>("/api/Transaction/Search");
        var ended = await _ops.GetFromJsonAsync<JsonElement>("/api/Transaction/Search?status=Ended");
        var byUid = await _ops.GetFromJsonAsync<JsonElement>("/api/Transaction/Search?search=TX-2");
        var csv = await _ops.GetStringAsync("/api/Transaction/Export?status=Ended");

        Assert.Equal(2, all.GetProperty("total").GetInt32());
        Assert.Equal("TX-2", all.GetProperty("items")[0].GetProperty("uid").GetString()); // newest first
        Assert.Equal("Agdal", all.GetProperty("items")[0].GetProperty("stationName").GetString());
        Assert.Equal(1, ended.GetProperty("total").GetInt32());
        Assert.Equal(_f.ActiveId, byUid.GetProperty("items")[0].GetProperty("chargingSessionId").GetInt32());
        var rows = csv.TrimStart('﻿').Trim().Split('\n');
        Assert.Equal(2, rows.Length);
        Assert.Contains(";1000;13000;12;OPS-00001234;", rows[1]);
    }

    [Fact]
    public async Task The_session_page_answers_404_for_an_unknown_session()
    {
        var info = await _ops.GetAsync("/api/ChargingSession/GetChargingSessionInformations/987654");
        var invoice = await _ops.GetAsync("/api/ChargingSession/GetChargingSessionInvoice/987654");
        var known = await _ops.GetFromJsonAsync<JsonElement>($"/api/ChargingSession/GetChargingSessionInformations/{_f.BilledId}");

        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, invoice.StatusCode);
        Assert.Equal(30m, known.GetProperty("energyPriceWithVAT").GetDecimal());
    }
}
