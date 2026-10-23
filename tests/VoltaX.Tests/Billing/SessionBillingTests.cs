using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Billing;

/// <summary>
/// One formula from start to invoice: what the charger and the app are told, what the card is debited and what
/// the session stores are the same amounts (flat fee + energy + time, idle time after the grace period).
/// </summary>
public class SessionBillingTests
{
    private const string Cp = "CP-BILL";
    private static readonly DateTime T0 = Seeds.Now;

    /// <summary>2.50/kWh, 0.10/min, 1.00 per session, 0.50 per idle minute, 5 minutes grace, VAT 20%.</summary>
    private static async Task<(OcppTestHost Host, int CardId)> Station(decimal balance = 100m, string protocol = OcppTestHost.Ocpp201)
    {
        var host = new OcppTestHost(relational: true);
        var cardId = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c =>
            {
                c.PricePerKWh = 2.50m;
                c.PricePerMinute = 0.10m;
                c.FlatFee = 1.00m;
                c.PricePerIdleMinute = 0.50m;
            });
            var user = await Seeds.User(db);
            var card = Seeds.Card("TAG-1", balance, user.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            cardId = card.ID;
        });
        host.Sender.Versions[Cp] = protocol;
        return (host, cardId);
    }

    private static Task<ChargingSession> Session(OcppTestHost host) => host.Query(db => db.ChargingSessions.SingleAsync());
    private static Task<decimal> Balance(OcppTestHost host, int cardId) =>
        host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync());

    [Fact]
    public async Task Ocpp201_session_is_billed_energy_time_and_flat_fee_once()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);

        Assert.Equal("Accepted", await charger.Started("TX-1", "TAG-1", 1000, T0));
        await charger.Updated("TX-1", 6000, T0.AddMinutes(10));
        await charger.Ended("TX-1", 13000, T0.AddMinutes(30));

        var session = await Session(host);
        Assert.Equal(12, session.ChargedKwhs!.Value, 6);
        Assert.Equal(30, session.ChargedMinutes!.Value, 6);
        Assert.Equal(30.00m, session.EnergyAmount);
        Assert.Equal(3.00m, session.TimeAmount);
        Assert.Equal(1.00m, session.FlatFeeAmount);
        Assert.Equal(34.00m, session.TotalAmount);
        Assert.Equal(100m, session.BalanceBefore);
        Assert.Equal(66m, session.BalanceAfter);
        Assert.Equal(66m, await Balance(host, cardId));

        var debit = await host.Query(db => db.WalletTransactions.SingleAsync(w => w.Type == WalletTransactionType.SessionCharge));
        Assert.Equal(-34.00m, debit.Amount);
        Assert.Equal(session.ID, debit.ChargingSessionID);
        Assert.Equal(34.00m, await host.Query(db => db.Transactions.Select(t => t.Amount).SingleAsync()));

        // The charger screen gets the same amount.
        Assert.True(await host.Sender.WaitFor("CostUpdated"));
        Assert.Contains("\"totalCost\":34.0", host.Sender.Sent.Last(s => s.Action == "CostUpdated").Payload);
    }

    [Fact]
    public async Task A_price_change_during_the_session_does_not_change_its_bill()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-1", "TAG-1", 0, T0);

        await host.Seed(async db => (await db.Connectors.SingleAsync()).PricePerKWh = 9.99m);
        await charger.Ended("TX-1", 10000, T0.AddMinutes(10));

        var session = await Session(host);
        Assert.Equal(2.50m, session.PricePerKWh);
        Assert.Equal(25.00m, session.EnergyAmount);
        Assert.Equal(100m - 25m - 1m - 1m, await Balance(host, cardId));
    }

    [Fact]
    public async Task A_retried_Ended_event_is_not_billed_twice()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-1", "TAG-1", 0, T0);
        await charger.Ended("TX-1", 4000, T0.AddMinutes(20), seqNo: 7);
        var after = await Balance(host, cardId);

        await charger.Ended("TX-1", 4000, T0.AddMinutes(20), seqNo: 7);

        Assert.Equal(after, await Balance(host, cardId));
        Assert.Equal(1, await host.Query(db => db.WalletTransactions.CountAsync(w => w.Type == WalletTransactionType.SessionCharge)));
    }

    [Fact]
    public async Task No_energy_means_no_flat_fee()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-1", "TAG-1", 500, T0);
        await charger.Ended("TX-1", 500, T0.AddMinutes(2), stoppedReason: "EVDisconnected", triggerReason: "EVCommunicationLost");

        var session = await Session(host);
        Assert.Equal(0m, session.FlatFeeAmount);
        Assert.Equal(0.20m, session.TotalAmount);
        Assert.Equal(99.80m, await Balance(host, cardId));
    }

    [Fact]
    public async Task Idle_time_beyond_the_grace_period_is_billed_when_the_car_leaves()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-1", 0, T0);
        await charger.Ended("TX-1", 10000, T0.AddMinutes(60), stoppedReason: "Local");

        await charger.Status("Available", T0.AddMinutes(80));

        var session = await Session(host);
        Assert.Equal(20, session.IdleMinutes!.Value, 6);
        Assert.Equal(10.00m, session.IdleAmount);
        Assert.Equal(25m + 6m + 1m + 10m, session.TotalAmount);
        Assert.Equal(ChargingSessionStatusEnum.Completed, session.ChargingSessionStatus);
        Assert.Equal(100m - 42m, await Balance(host, cardId));
        var idle = await host.Query(db => db.WalletTransactions.SingleAsync(w => w.Type == WalletTransactionType.IdleFee));
        Assert.Equal(-10.00m, idle.Amount);
    }

    [Fact]
    public async Task Idle_time_within_the_grace_period_is_free()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-1", 0, T0);
        await charger.Ended("TX-1", 1000, T0.AddMinutes(10));

        await charger.Status("Available", T0.AddMinutes(14));

        var session = await Session(host);
        Assert.Equal(0m, session.IdleAmount);
        Assert.False(await host.Query(db => db.WalletTransactions.AnyAsync(w => w.Type == WalletTransactionType.IdleFee)));
        Assert.Equal(100m - 2.5m - 1m - 1m, await Balance(host, cardId));
    }

    [Fact]
    public async Task Ocpp16_session_uses_the_same_formula()
    {
        var (host, cardId) = await Station(protocol: OcppTestHost.Ocpp16);
        await using var _ = host;
        var charger = new Charger16(host, Cp);
        await charger.Boot();
        await charger.Status(1, "Preparing", at: T0);

        var (status, transactionId) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(transactionId, 7000, T0.AddMinutes(15), powerW: 22000);
        await charger.Stop(transactionId, 13000, T0.AddMinutes(30), "EVDisconnected");

        Assert.Equal("Accepted", status);
        var session = await Session(host);
        Assert.Equal(34.00m, session.TotalAmount);
        Assert.Equal(66m, await Balance(host, cardId));
    }

    [Fact]
    public async Task The_session_is_stopped_remotely_when_the_running_cost_reaches_the_balance()
    {
        var (host, cardId) = await Station(balance: 10m);
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-1", "TAG-1", 0, T0);

        await charger.Updated("TX-1", 2000, T0.AddMinutes(5)); // 5.00 + 0.50 + 1.00 = 6.50, still covered
        Assert.False(host.Sender.Sent.Any(s => s.Action == "RequestStopTransaction"));
        await charger.Updated("TX-1", 4000, T0.AddMinutes(10)); // 10.00 + 1.00 + 1.00 = 12.00 > 10

        Assert.True(await host.Sender.WaitFor("RequestStopTransaction"));
        Assert.Contains("TX-1", host.Sender.Sent.First(s => s.Action == "RequestStopTransaction").Payload);
        Assert.True(await host.Query(db => db.Notifications.AnyAsync(n => n.Action == "LowBalance")));
    }

    [Fact]
    public async Task A_session_costing_more_than_the_balance_goes_negative_and_raises_a_system_report()
    {
        var (host, cardId) = await Station(balance: 5m);
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-1", "TAG-1", 0, T0);
        // Offline: no update arrived in time to stop it.
        await charger.Ended("TX-1", 4000, T0.AddMinutes(10), offline: true);

        Assert.Equal(5m - 12m, await Balance(host, cardId));
        Assert.Single(host.SystemReports.CallsTo(nameof(ISystemReportService.HandleReport)));
    }

    [Theory]
    [InlineData(12.0, 30.0, 30.00, 3.00, 1.00)]
    [InlineData(0.333, 0.5, 0.83, 0.05, 1.00)]   // 0.8325 -> 0.83, 0.05 exact
    [InlineData(1.0, 0.25, 2.50, 0.03, 1.00)]    // 0.025 -> 0.03 (half away from zero)
    [InlineData(0.0, 10.0, 0.00, 1.00, 0.00)]    // no energy, no flat fee
    public void Each_component_is_rounded_to_the_cent(double kwh, double minutes, double energy, double time, double flat)
    {
        var tariff = new Tariff(2.50m, 0.10m, 0.50m, 1.00m, 0.2m);

        var charge = CostCalculator.Instance.Charge(tariff, kwh, minutes);

        Assert.Equal((decimal)energy, charge.Energy);
        Assert.Equal((decimal)time, charge.Time);
        Assert.Equal((decimal)flat, charge.FlatFee);
        Assert.Equal(charge.Energy + charge.Time + charge.FlatFee, charge.Total);
    }

    [Fact]
    public void Vat_is_split_out_of_the_included_amount()
    {
        var (excl, vat) = CostCalculator.SplitVat(34.00m, 0.2m);

        Assert.Equal(28.33m, excl);
        Assert.Equal(5.67m, vat);
        Assert.Equal(34.00m, excl + vat);
    }

    [Fact]
    public void Idle_fee_applies_to_the_whole_idle_time_after_the_grace_period()
    {
        var tariff = new Tariff(0, 0, 0.50m, 0, 0.2m);

        Assert.Equal(0m, CostCalculator.Instance.IdleFee(tariff, 5, gracePeriodSeconds: 300));
        Assert.Equal(3.00m, CostCalculator.Instance.IdleFee(tariff, 6, gracePeriodSeconds: 300));
    }
}
