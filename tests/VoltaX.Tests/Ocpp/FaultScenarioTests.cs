using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Ocpp;

/// <summary>Faults, power cuts and chargers that never end their transaction.</summary>
public class FaultScenarioTests
{
    private const string Cp = "CP-FAULT";
    private static readonly DateTime T0 = Seeds.Now;

    private static async Task<(OcppTestHost Host, int CardId)> Station()
    {
        var host = new OcppTestHost(relational: true);
        var cardId = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c => { c.PricePerKWh = 2m; c.PricePerMinute = 0m; c.FlatFee = 0m; c.PricePerIdleMinute = 0m; });
            var user = await Seeds.User(db);
            var card = Seeds.Card("TAG-1", 100m, user.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            cardId = card.ID;
        });
        host.Sender.Versions[Cp] = OcppTestHost.Ocpp201;
        return (host, cardId);
    }

    private static Task<Transaction> Tx(OcppTestHost host) => host.Query(db => db.Transactions.SingleAsync());
    private static Task<decimal> Balance(OcppTestHost host, int cardId) =>
        host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync());
    private static Task<int> Sweep(OcppTestHost host, TimeSpan after) =>
        host.Provider.GetRequiredService<StaleTransactionSweeper>().CloseDueAsync(DateTime.UtcNow + after);

    [Fact]
    public async Task A_fault_during_charging_warns_the_driver_and_the_charger_ends_it_on_the_measured_energy()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-1", 0, T0);
        await charger.Updated("TX-1", 3000, T0.AddMinutes(10));

        await charger.Status("Faulted", T0.AddMinutes(11));
        await charger.Ended("TX-1", 3200, T0.AddMinutes(12), triggerReason: "AbnormalCondition", stoppedReason: "OvercurrentFault");

        Assert.Single(host.SystemReports.CallsTo(nameof(ISystemReportService.HandleReport)));
        var notifications = await host.Query(db => db.Notifications.Select(n => n.Action).ToListAsync());
        Assert.Contains("ChargerFaultDuringSession", notifications);
        Assert.Contains("ChargingEndedAbnormally", notifications);
        Assert.Equal(TransactionStatusEnum.Ended, (await Tx(host)).Status);
        Assert.Equal(100m - 6.40m, await Balance(host, cardId));
    }

    [Fact]
    public async Task A_faulted_charger_that_never_ends_the_transaction_has_it_closed_on_the_last_meter_value()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-1", 0, T0);
        await charger.Updated("TX-1", 5000, T0.AddMinutes(20));
        await charger.Status("Faulted", T0.AddMinutes(21));

        Assert.Equal(0, await Sweep(host, TimeSpan.FromMinutes(30)));   // still within the wait
        Assert.Equal(1, await Sweep(host, TimeSpan.FromMinutes(61)));

        var tx = await Tx(host);
        Assert.Equal(TransactionStatusEnum.Ended, tx.Status);
        Assert.Equal("ConnectorFaulted", tx.StopReason);
        Assert.Equal(T0.AddMinutes(20), tx.StopTime);
        Assert.Equal(5.0, tx.MeterStop);
        Assert.Equal(90m, await Balance(host, cardId));

        // The charger recovers and finally sends its Ended: acknowledged, not billed again.
        await charger.Ended("TX-1", 5000, T0.AddMinutes(20), triggerReason: "AbnormalCondition", stoppedReason: "PowerLoss");
        Assert.Equal(90m, await Balance(host, cardId));
    }

    [Fact]
    public async Task A_transaction_forgotten_after_a_power_cut_is_closed_after_the_stale_delay()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger16(host, Cp);
        host.Sender.Versions[Cp] = OcppTestHost.Ocpp16;
        await charger.Boot();
        await charger.Status(1, "Preparing", at: T0);
        var (_, id) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(id, 9000, T0.AddMinutes(40));
        // Power cut: the charger reboots and never mentions the transaction again.
        await charger.Boot();
        await charger.Status(1, "Available", at: T0.AddMinutes(50));

        Assert.Equal(0, await Sweep(host, TimeSpan.FromHours(11)));
        Assert.Equal(1, await Sweep(host, TimeSpan.FromHours(13)));

        var tx = await Tx(host);
        Assert.Equal("StaleTransaction", tx.StopReason);
        Assert.Equal(100m - 16m, await Balance(host, cardId));
    }

    [Fact]
    public async Task A_long_session_that_keeps_reporting_is_never_closed()
    {
        var (host, _) = await Station();
        await using var __ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Occupied", T0);
        await charger.Started("TX-1", "TAG-1", 0, T0);
        await charger.Updated("TX-1", 40000, T0.AddHours(9));

        Assert.Equal(0, await Sweep(host, TimeSpan.FromHours(2)));
        Assert.Equal(TransactionStatusEnum.Current, (await Tx(host)).Status);
    }
}
