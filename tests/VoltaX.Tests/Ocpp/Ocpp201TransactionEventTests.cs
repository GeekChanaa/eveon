using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaX.Tests.Ocpp.Matrix;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp;

/// <summary>
/// OCPP 2.0.1 TransactionEvent specifics on the matrix station: idTokenInfo, seqNo, trigger reasons, transactions
/// started before the token or the EVSE is known, meter units.
/// </summary>
public class Ocpp201TransactionEventTests
{
    private static async Task<(OcppTestHost Host, ScenarioCharger201 Charger, int CardId)> Open201(decimal balance = 100m)
    {
        var (host, charger, cardId) = await Open(OcppTestHost.Ocpp201, balance);
        return (host, (ScenarioCharger201)charger, cardId);
    }

    [Fact]
    public async Task An_event_without_idToken_is_answered_without_idTokenInfo()
    {
        var (host, charger, _) = await Open201();
        await using var __ = host;
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);

        var updated = await charger.Event("Updated", uid, "MeterValuePeriodic", T0.AddMinutes(5), 3000, chargingState: "Charging");
        var ended = await charger.Event("Ended", uid, "EVCommunicationLost", T0.AddMinutes(9), 4000, stoppedReason: "EVDisconnected");
        var unknown = await charger.Event("Updated", "TX-NOBODY-KNOWS", "MeterValuePeriodic", T0.AddMinutes(9), 4000);

        Assert.False(updated.TryGetProperty("idTokenInfo", out _));
        Assert.False(ended.TryGetProperty("idTokenInfo", out _));
        Assert.False(unknown.TryGetProperty("idTokenInfo", out _));
    }

    [Fact]
    public async Task Two_chargers_using_the_same_transaction_id_never_touch_each_other()
    {
        // Transaction ids are only unique per charging station: many chargers count from 1.
        await using var host = new OcppTestHost(relational: true);
        int cardA = 0, cardB = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, "CP-A", c => { c.PricePerKWh = 2m; c.PricePerMinute = 0m; c.FlatFee = 0m; },
                (await Seeds.Station(db, name: "Station A")).ID);
            await Seeds.ChargePoint(db, "CP-B", c => { c.PricePerKWh = 2m; c.PricePerMinute = 0m; c.FlatFee = 0m; },
                (await Seeds.Station(db, name: "Station B")).ID);
            var a = Seeds.Card("TAG-A", 100m, (await Seeds.User(db, "a@example.com")).ID);
            var b = Seeds.Card("TAG-B", 100m, (await Seeds.User(db, "b@example.com")).ID);
            db.Cards.AddRange(a, b);
            await db.SaveChangesAsync();
            (cardA, cardB) = (a.ID, b.ID);
        });
        var chargerA = new Charger201(host, "CP-A");
        var chargerB = new Charger201(host, "CP-B");
        await chargerA.Boot();
        await chargerB.Boot();

        await chargerA.Started("1", "TAG-A", 0, T0);
        await chargerB.Started("1", "TAG-B", 0, T0.AddMinutes(5));
        await chargerA.Updated("1", 4000, T0.AddMinutes(10));
        await chargerA.Ended("1", 5000, T0.AddMinutes(20));

        var transactions = await host.Query(db => db.Transactions.Include(t => t.Connector).ThenInclude(c => c!.ChargePoint).ToListAsync());
        var onA = transactions.Single(t => t.Connector!.ChargePoint!.ChargePointId == "CP-A");
        var onB = transactions.Single(t => t.Connector!.ChargePoint!.ChargePointId == "CP-B");
        Assert.Equal(TransactionStatusEnum.Ended, onA.Status);
        Assert.Equal(5.0, onA.MeterStop);
        Assert.Equal(TransactionStatusEnum.Current, onB.Status);
        Assert.Null(onB.StopTime);
        Assert.Equal(0, onB.MeterStop ?? 0);
        Assert.Equal(90m, await ScenarioAssert.Balance(host, cardA));
        Assert.Equal(100m, await ScenarioAssert.Balance(host, cardB));
    }
}
