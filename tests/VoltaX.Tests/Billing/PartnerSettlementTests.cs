using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;
using VoltaXApi.Services;
using VoltaXApi.Services.Billing;

namespace VoltaX.Tests.Billing;

/// <summary>
/// A network with two partners, sessions last month and this month. Partner A keeps 90 % of the net revenue and
/// pays 0.20 MAD excl. VAT per kWh.
/// </summary>
public sealed class SettlementNetwork : IAsyncDisposable
{
    public ApiFactory Api { get; } = new(new Dictionary<string, string?> { ["Settlements:GenerateOnDay"] = "1" });
    public User Admin { get; private set; } = null!;
    public User Customer { get; private set; } = null!;
    public User PartnerUserA { get; private set; } = null!;
    public User PartnerUserB { get; private set; } = null!;
    public int PartnerA { get; private set; }
    public int PartnerB { get; private set; }
    public int CardId { get; private set; }
    public int S1 { get; private set; }
    public int S2 { get; private set; }
    public int S3 { get; private set; }
    public int ConnectorA { get; private set; }
    /// <summary>Last business month (fully past) and the current one.</summary>
    public (int Year, int Month) Previous { get; private set; }
    public (int Year, int Month) Current { get; private set; }

    public static async Task<SettlementNetwork> Create()
    {
        var network = new SettlementNetwork();
        await network.Seed();
        return network;
    }

    private async Task Seed()
    {
        var clock = Api.Services.GetRequiredService<IBusinessClock>();
        var previous = clock.Today.AddMonths(-1);
        Previous = (previous.Year, previous.Month);
        Current = (clock.Today.Year, clock.Today.Month);
        var previousStart = clock.StartOfMonthUtc(previous.Year, previous.Month);
        var currentStart = clock.StartOfMonthUtc(clock.Today.Year, clock.Today.Month);

        await Api.Seed(async db =>
        {
            Admin = await Seeds.User(db, "settle-admin@test.ma", role: "Admin");
            Customer = await Seeds.User(db, "settle-driver@test.ma");
            var a = new Partner { Name = "Alpha Energie", Description = "", Email = "a@alpha.ma", Phone = "0600000101", PartnerIdentificationNumber = "P-A",
                CommissionRate = 0.10m, CommissionPerKwh = 0.20m };
            var b = new Partner { Name = "Beta Charge", Description = "", Email = "b@beta.ma", Phone = "0600000102", PartnerIdentificationNumber = "P-B",
                CommissionRate = 0.05m };
            db.Partners.AddRange(a, b);
            await db.SaveChangesAsync();
            (PartnerA, PartnerB) = (a.ID, b.ID);
            PartnerUserA = await Seeds.User(db, "alpha@test.ma", role: "Partner", partnerId: a.ID);
            PartnerUserB = await Seeds.User(db, "beta@test.ma", role: "Partner", partnerId: b.ID);
            await Seeds.ChargePoint(db, "CP-ALPHA", c => c.PricePerKWh = 2m, (await Seeds.Station(db, a.ID, "Alpha Anfa")).ID);
            await Seeds.ChargePoint(db, "CP-BETA", c => c.PricePerKWh = 2m, (await Seeds.Station(db, b.ID, "Beta Maarif")).ID);
            var connectors = await db.Connectors.Include(c => c.ChargePoint).ToListAsync();
            ConnectorA = connectors.Single(c => c.ChargePoint!.ChargePointId == "CP-ALPHA").ID;
            var connectorB = connectors.Single(c => c.ChargePoint!.ChargePointId == "CP-BETA").ID;
            var card = Seeds.Card("SETTLE-1", 500m, Customer.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            CardId = card.ID;

            ChargingSession Session(int connector, DateTime start, double kwh, decimal total) => new()
            {
                ConnectorID = connector, CardID = card.ID, UserID = Customer.ID, StartDate = start, EndDate = start.AddMinutes(30), ChargedKwhs = kwh,
                ChargedMinutes = 30, VatRate = 0.2m, PricePerKWh = 2m, EnergyAmount = total, TimeAmount = 0, FlatFeeAmount = 0, IdleAmount = 0,
                IdleMinutes = 0, TotalAmount = total, BilledAt = start.AddMinutes(30), ChargingSessionStatus = ChargingSessionStatusEnum.Completed
            };
            var s1 = Session(ConnectorA, previousStart.AddDays(9), 12.345, 34.00m);
            var s2 = Session(ConnectorA, previousStart.AddDays(19), 10, 20.00m);
            var s3 = Session(ConnectorA, currentStart.AddSeconds(5), 7.5, 15.00m);
            var other = Session(connectorB, previousStart.AddDays(3), 50, 100.00m);
            db.ChargingSessions.AddRange(s1, s2, s3, other);
            await db.SaveChangesAsync();
            (S1, S2, S3) = (s1.ID, s2.ID, s3.ID);
        });
    }

    public async Task<T> With<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Api.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public Task<PartnerSettlement?> Generate(int partnerId, (int Year, int Month) period) =>
        With(sp => sp.GetRequiredService<PartnerSettlementService>().GenerateAsync(partnerId, period.Year, period.Month));

    public ValueTask DisposeAsync()
    {
        Api.Dispose();
        return ValueTask.CompletedTask;
    }
}

public class PartnerSettlementTests
{
    [Fact]
    public async Task A_month_settles_the_partners_sessions_with_commission_and_its_vat()
    {
        await using var network = await SettlementNetwork.Create();

        var settlement = await network.Generate(network.PartnerA, network.Previous);

        Assert.NotNull(settlement);
        var lines = await network.Api.Db(db => db.Set<PartnerSettlementLine>().Where(l => l.SettlementID == settlement!.ID).OrderBy(l => l.Date).ToListAsync());
        Assert.Equal(new[] { $"session:{network.S1}", $"session:{network.S2}" }, lines.Select(l => l.SourceKey));
        // 34.00 TTC: 28.33 HT, commission 2.83 + 12.345 kWh x 0.20 = 2.47, VAT 1.06, payout 27.64
        Assert.Equal((34.00m, 28.33m, 5.67m, 5.30m, 1.06m, 27.64m), (lines[0].Gross, lines[0].NetExclVat, lines[0].Vat, lines[0].CommissionExclVat, lines[0].CommissionVat, lines[0].NetPayout));
        Assert.Equal((20.00m, 16.67m, 3.33m, 3.67m, 0.73m, 15.60m), (lines[1].Gross, lines[1].NetExclVat, lines[1].Vat, lines[1].CommissionExclVat, lines[1].CommissionVat, lines[1].NetPayout));

        Assert.Equal(2, settlement!.SessionCount);
        Assert.Equal(22.345m, settlement.EnergyKwh);
        Assert.Equal(lines.Sum(l => l.Gross), settlement.GrossAmount);
        Assert.Equal(lines.Sum(l => l.NetExclVat), settlement.NetExclVat);
        Assert.Equal(lines.Sum(l => l.Vat), settlement.RevenueVat);
        Assert.Equal(lines.Sum(l => l.CommissionExclVat), settlement.CommissionExclVat);
        Assert.Equal(lines.Sum(l => l.CommissionVat), settlement.CommissionVat);
        Assert.Equal(43.24m, settlement.NetPayout);
        Assert.Equal(settlement.GrossAmount - settlement.CommissionExclVat - settlement.CommissionVat, settlement.NetPayout);
        Assert.Equal((0.10m, 0.20m, 0.2m), (settlement.CommissionRate, settlement.CommissionPerKwh, settlement.VatRate));
        Assert.Equal(SettlementStatus.Pending, settlement.Status);
    }

    [Fact]
    public async Task A_session_is_settled_once_and_late_ones_go_to_the_next_month()
    {
        await using var network = await SettlementNetwork.Create();
        var previous = await network.Generate(network.PartnerA, network.Previous);
        await network.With(sp => sp.GetRequiredService<PartnerSettlementService>()
            .MarkPaidAsync(previous!.ID, new MarkSettlementPaidRequest("VIR-2026-001"), network.Admin.ID));
        // A session of last month reported after it was paid (charger offline).
        int late = 0;
        await network.Api.Seed(async db =>
        {
            var s1 = await db.ChargingSessions.AsNoTracking().SingleAsync(s => s.ID == network.S1);
            var copy = new ChargingSession
            {
                ConnectorID = s1.ConnectorID, CardID = s1.CardID, UserID = s1.UserID, StartDate = s1.StartDate.AddDays(5), EndDate = s1.EndDate!.Value.AddDays(5),
                ChargedKwhs = 5, VatRate = 0.2m, EnergyAmount = 12m, TimeAmount = 0, FlatFeeAmount = 0, IdleAmount = 0, TotalAmount = 12m,
                ChargingSessionStatus = ChargingSessionStatusEnum.Completed
            };
            db.ChargingSessions.Add(copy);
            await db.SaveChangesAsync();
            late = copy.ID;
        });

        var again = await network.Generate(network.PartnerA, network.Previous);
        var current = await network.Generate(network.PartnerA, network.Current);

        Assert.Equal(SettlementStatus.Paid, again!.Status);
        Assert.Equal(54m, again.GrossAmount); // frozen
        var keys = await network.Api.Db(db => db.Set<PartnerSettlementLine>().Where(l => l.SettlementID == current!.ID).Select(l => l.SourceKey).ToListAsync());
        Assert.Equal(new[] { $"session:{late}", $"session:{network.S3}" }.OrderBy(k => k), keys.OrderBy(k => k));
        Assert.Equal(1, await network.Api.Db(db => db.Set<PartnerSettlementLine>().CountAsync(l => l.SourceKey == $"session:{network.S1}")));
    }

    [Fact]
    public async Task A_refund_of_a_settled_session_is_deducted_from_the_next_settlement()
    {
        await using var network = await SettlementNetwork.Create();
        await network.Generate(network.PartnerA, network.Previous);
        await network.With(sp => sp.GetRequiredService<RefundService>()
            .RefundSessionAsync(network.S1, new RefundRequest(6.00m, "Faulty connector", "settle-refund-1"), network.Admin.ID));

        var current = await network.Generate(network.PartnerA, network.Current);

        var lines = await network.Api.Db(db => db.Set<PartnerSettlementLine>().Where(l => l.SettlementID == current!.ID).ToListAsync());
        var refund = Assert.Single(lines, l => l.RefundID != null);
        Assert.Equal(-6.00m, refund.Gross);
        Assert.Equal(-5.00m, refund.NetExclVat);
        Assert.Equal(-0.50m, refund.CommissionExclVat);
        Assert.Equal(-0.10m, refund.CommissionVat);
        Assert.Equal(-5.40m, refund.NetPayout);
        Assert.Equal(1, current!.SessionCount);
        Assert.Equal(15.00m - 6.00m, current.GrossAmount);
        Assert.Equal(lines.Sum(l => l.NetPayout), current.NetPayout);
    }

    [Fact]
    public async Task A_pending_settlement_is_recomputed_with_the_current_terms()
    {
        await using var network = await SettlementNetwork.Create();
        var first = await network.Generate(network.PartnerA, network.Previous);
        await network.With(sp => sp.GetRequiredService<PartnerSettlementService>()
            .UpdateCommissionAsync(network.PartnerA, new UpdateCommissionRequest(20m, 0m)));

        var second = await network.Generate(network.PartnerA, network.Previous);

        Assert.Equal(first!.ID, second!.ID);
        Assert.Equal(0.20m, second.CommissionRate);
        Assert.Equal(Math.Round(28.33m * 0.2m, 2) + Math.Round(16.67m * 0.2m, 2), second.CommissionExclVat);
        Assert.Equal(2, await network.Api.Db(db => db.Set<PartnerSettlementLine>().CountAsync(l => l.SettlementID == second.ID)));
    }

    [Fact]
    public async Task The_monthly_job_settles_last_month_for_every_partner_once()
    {
        await using var network = await SettlementNetwork.Create();

        var first = await network.With(sp => PartnerSettlementJob.RunAsync(sp));
        var second = await network.With(sp => PartnerSettlementJob.RunAsync(sp));

        Assert.Equal(new[] { network.PartnerA, network.PartnerB }.OrderBy(i => i), first.Select(s => s.PartnerID).OrderBy(i => i));
        Assert.All(first, s => Assert.Equal((network.Previous.Year, network.Previous.Month), (s.Year, s.Month)));
        Assert.Empty(second);
        var beta = first.Single(s => s.PartnerID == network.PartnerB);
        Assert.Equal(100m, beta.GrossAmount);
        Assert.Null(beta.GeneratedByUserID);
    }

    [Fact]
    public async Task Operators_generate_settlements_set_terms_and_record_payouts()
    {
        await using var network = await SettlementNetwork.Create();
        var admin = network.Api.Client(network.Admin);

        var terms = await admin.PutAsJsonAsync($"/api/Settlement/Commissions/{network.PartnerB}", new { commissionPercent = 12.5m, commissionPerKwh = 0.1m });
        var invalid = await admin.PutAsJsonAsync($"/api/Settlement/Commissions/{network.PartnerB}", new { commissionPercent = 120m, commissionPerKwh = 0m });
        var generated = await admin.PostAsJsonAsync("/api/Settlement/Generate", new { year = network.Previous.Year, month = network.Previous.Month });
        var list = await generated.Content.ReadFromJsonAsync<JsonElement>();
        var id = list.EnumerateArray().Single(s => s.GetProperty("partnerId").GetInt32() == network.PartnerB).GetProperty("id").GetInt32();
        var paid = await admin.PostAsJsonAsync($"/api/Settlement/{id}/Paid", new { reference = "VIR-778", paidAt = "2026-10-05T10:00:00Z" });
        var twice = await admin.PostAsJsonAsync($"/api/Settlement/{id}/Paid", new { reference = "VIR-779" });

        Assert.Equal(HttpStatusCode.OK, terms.StatusCode);
        Assert.Equal(0.125m, await network.Api.Db(db => db.Partners.Where(p => p.ID == network.PartnerB).Select(p => p.CommissionRate).SingleAsync()));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, paid.StatusCode);
        var dto = await paid.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Paid", dto.GetProperty("status").GetString());
        Assert.Equal("VIR-778", dto.GetProperty("paymentReference").GetString());
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        var search = await admin.GetFromJsonAsync<JsonElement>("/api/Settlement/Search?status=Paid");
        Assert.Equal(1, search.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await network.Api.Client(network.PartnerUserB).GetAsync("/api/Settlement/Search")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await network.Api.Client(network.Customer).PostAsJsonAsync("/api/Settlement/Generate",
            new { year = network.Previous.Year, month = network.Previous.Month })).StatusCode);
    }
}
