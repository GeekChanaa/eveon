using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Ocpp;

/// <summary>
/// Transaction life cycle edge cases seen on real chargers: retries, late and unknown events, events without
/// EVSE, offline transactions.
/// </summary>
public class TransactionScenarioTests
{
    private const string Cp = "CP-TX";
    private static readonly DateTime T0 = Seeds.Now;

    private static async Task<(OcppTestHost Host, int CardId)> Station(decimal balance = 200m)
    {
        var host = new OcppTestHost(relational: true);
        var cardId = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c => { c.PricePerKWh = 2m; c.PricePerMinute = 0m; c.FlatFee = 0m; });
            var user = await Seeds.User(db);
            var card = Seeds.Card("TAG-1", balance, user.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            cardId = card.ID;
        });
        host.Sender.Versions[Cp] = OcppTestHost.Ocpp201;
        return (host, cardId);
    }

    private static Task<Transaction> Tx(OcppTestHost host, string uid) => host.Query(db => db.Transactions.SingleAsync(t => t.Uid == uid));

    [Fact]
    public async Task A_late_Ended_for_a_closed_transaction_never_closes_the_current_one()
    {
        var (host, cardId) = await Station();
        await using var _ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-OLD", "TAG-1", 0, T0);
        await charger.Ended("TX-OLD", 5000, T0.AddMinutes(30));
        await charger.Started("TX-NEW", "TAG-1", 5000, T0.AddMinutes(40));
        var balance = await host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync());

        // The charger re-sends the old Ended (lost CALLRESULT), with a later meter value.
        await charger.Ended("TX-OLD", 9000, T0.AddMinutes(50));

        var current = await Tx(host, "TX-NEW");
        Assert.Equal(TransactionStatusEnum.Current, current.Status);
        Assert.Null(current.StopTime);
        Assert.Equal(5.0, (await Tx(host, "TX-OLD")).MeterStop);
        Assert.Equal(balance, await host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync()));
    }

    [Fact]
    public async Task Events_for_an_unknown_transaction_are_acknowledged_without_touching_others()
    {
        var (host, _) = await Station();
        await using var __ = host;
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-1", "TAG-1", 0, T0);

        await charger.Updated("TX-GHOST", 4000, T0.AddMinutes(5));
        await charger.Ended("TX-GHOST", 6000, T0.AddMinutes(9));

        var tx = await Tx(host, "TX-1");
        Assert.Equal(TransactionStatusEnum.Current, tx.Status);
        Assert.Equal(0, tx.MeterStop ?? 0);
        Assert.False(await host.Query(db => db.Transactions.AnyAsync(t => t.Uid == "TX-GHOST")));
    }
}
