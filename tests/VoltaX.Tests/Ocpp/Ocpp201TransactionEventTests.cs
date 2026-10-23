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
    public async Task A_transaction_started_on_plug_in_is_billed_from_the_authorization()
    {
        // TxStartPoint EVConnected: Started at plug-in without a token, the card comes in an Updated event, which
        // (like the later ones) may omit the EVSE.
        var (host, charger, cardId) = await Open201();
        await using var __ = host;
        await charger.Plug(T0);

        var started = await charger.Event("Started", "TX-P", "CablePluggedIn", T0, 1000, chargingState: "EVConnected");
        Assert.False(started.TryGetProperty("idTokenInfo", out _));
        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));

        var authorized = await charger.Event("Updated", "TX-P", "Authorized", T0.AddMinutes(2), null, idToken: "TAG-1", chargingState: "Charging",
            withEvse: false);
        Assert.Equal("Accepted", ScenarioCharger201.TokenStatus(authorized));
        await charger.Event("Updated", "TX-P", "MeterValuePeriodic", T0.AddMinutes(12), 6000, chargingState: "Charging", withEvse: false);
        await charger.Event("Ended", "TX-P", "StopAuthorized", T0.AddMinutes(32), 13000, idToken: "TAG-1", stoppedReason: "Local", withEvse: false);

        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(T0.AddMinutes(2), session.StartDate);
        Assert.Equal(1.0, (await Tx(host)).MeterStart);
        Assert.False(await host.Query(db => db.OcppPendingTransactions.AnyAsync()));
    }

    [Fact]
    public async Task A_transaction_started_on_plug_in_and_never_authorized_is_not_billed()
    {
        var (host, charger, cardId) = await Open201();
        await using var __ = host;
        await charger.Plug(T0);

        await charger.Event("Started", "TX-P", "CablePluggedIn", T0, 1000, chargingState: "EVConnected");
        await charger.Event("Updated", "TX-P", "MeterValuePeriodic", T0.AddMinutes(5), 1000, chargingState: "EVConnected", withEvse: false);
        var refused = await charger.Event("Updated", "TX-P", "Authorized", T0.AddMinutes(6), null, idToken: "BLOCKED", withEvse: false);
        await charger.Event("Ended", "TX-P", "Deauthorized", T0.AddMinutes(7), 1000, stoppedReason: "DeAuthorized", withEvse: false);

        Assert.Equal("Blocked", ScenarioCharger201.TokenStatus(refused));
        await ScenarioAssert.NothingBilled(host, cardId, 100m);
        Assert.False(await host.Query(db => db.OcppPendingTransactions.AnyAsync()));
    }

    [Fact]
    public async Task A_repeated_Started_after_a_plug_in_start_does_not_restart_the_wait()
    {
        var (host, charger, cardId) = await Open201();
        await using var __ = host;
        await charger.Event("Started", "TX-P", "CablePluggedIn", T0, 1000, chargingState: "EVConnected", seqNo: 0);
        await charger.Event("Updated", "TX-P", "Authorized", T0.AddMinutes(1), 1000, idToken: "TAG-1", chargingState: "Charging", withEvse: false);

        // The Started CALLRESULT was lost: the charger sends it again after the session started.
        await charger.Event("Started", "TX-P", "CablePluggedIn", T0, 1000, chargingState: "EVConnected", seqNo: 0);
        await charger.Event("Ended", "TX-P", "EVCommunicationLost", T0.AddMinutes(31), 13000, stoppedReason: "EVDisconnected", withEvse: false);

        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.False(await host.Query(db => db.OcppPendingTransactions.AnyAsync()));
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
