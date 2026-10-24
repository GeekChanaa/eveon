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
    public async Task A_transaction_authorized_before_plug_in_starts_with_the_first_event_naming_its_EVSE()
    {
        var (host, charger, cardId) = await Open201();
        await using var __ = host;

        var started = await charger.Event("Started", "TX-A", "Authorized", T0, 1000, idToken: "TAG-1", withEvse: false);
        Assert.Equal("Accepted", ScenarioCharger201.TokenStatus(started));
        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));

        await charger.Plug(T0.AddMinutes(1));
        await charger.Event("Updated", "TX-A", "CablePluggedIn", T0.AddMinutes(1), 1000, chargingState: "Charging");
        await charger.Event("Ended", "TX-A", "StopAuthorized", T0.AddMinutes(30), 13000, idToken: "TAG-1", stoppedReason: "Local", withEvse: false);

        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(T0, session.StartDate);
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
    public async Task Sequence_number_gaps_and_every_kind_of_trigger_reason_are_processed()
    {
        var (host, charger, cardId) = await Open201();
        await using var __ = host;
        await charger.Event("Started", "TX-S", "Authorized", T0, 1000, idToken: "TAG-1", chargingState: "Charging", seqNo: 0);

        // seqNo 2, 3, 6-8 never arrive (lost while offline): the CSMS carries on with what it gets.
        await charger.Event("Updated", "TX-S", "MeterValuePeriodic", T0.AddMinutes(5), 2000, chargingState: "Charging", seqNo: 1);
        await charger.Event("Updated", "TX-S", "ChargingRateChanged", T0.AddMinutes(8), 3000, chargingState: "Charging", seqNo: 4);
        await charger.Event("Updated", "TX-S", "MeterValueClock", T0.AddMinutes(15), 6000, chargingState: "Charging", seqNo: 5);
        await charger.Event("Updated", "TX-S", "SignedDataReceived", T0.AddMinutes(20), 9000, chargingState: "Charging", seqNo: 9);
        await charger.Event("Updated", "TX-S", "EnergyLimitReached", T0.AddMinutes(25), 13000, chargingState: "SuspendedEVSE", seqNo: 10);
        await charger.Event("Updated", "TX-S", "ChargingStateChanged", T0.AddMinutes(26), 13000, chargingState: "SuspendedEVSE", seqNo: 11);
        await charger.Event("Ended", "TX-S", "EnergyLimitReached", T0.AddMinutes(30), 13000, stoppedReason: "EnergyLimitReached", seqNo: 12);

        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(ReasonEnumType.EnergyLimitReached, session.StoppedReason);
        Assert.DoesNotContain("ChargingEndedAbnormally", await ScenarioAssert.Notifications(host));
    }

    [Fact]
    public async Task A_lost_EV_communication_without_a_stop_reason_is_reported_as_abnormal()
    {
        var (host, charger, cardId) = await Open201();
        await using var __ = host;
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);

        await charger.Event("Ended", uid, "EVCommunicationLost", T0.AddMinutes(30), 13000);

        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Contains("ChargingEndedAbnormally", await ScenarioAssert.Notifications(host));
    }

    private static object Sample(double value, string? unit, int multiplier = 0, string? context = null, string? phase = null,
        string measurand = "Energy.Active.Import.Register") =>
        new { value, context, measurand, phase, unitOfMeasure = unit == null ? null : new { unit, multiplier } };

    private static Task<System.Text.Json.JsonElement> EventWithSamples(OcppTestHost host, string eventType, string triggerReason, DateTime at, int seqNo,
        params object[] samples) =>
        host.Call(Cp, "TransactionEvent", new
        {
            eventType,
            timestamp = at.ToString("o"),
            triggerReason,
            seqNo,
            transactionInfo = new { transactionId = "TX-U", chargingState = "Charging", stoppedReason = eventType == "Ended" ? "Local" : null },
            evse = new { id = 1, connectorId = 1 },
            idToken = eventType == "Started" ? new { idToken = "TAG-1", type = "ISO14443" } : null,
            meterValue = new[] { new { timestamp = at.ToString("o"), sampledValue = samples } }
        });

    [Fact]
    public async Task Meter_values_in_kWh_Wh_and_with_negative_multipliers_are_normalized()
    {
        var (host, _, cardId) = await Open201();
        await using var __ = host;

        // 1 kWh as kWh.
        await EventWithSamples(host, "Started", "Authorized", T0, 0, Sample(1, "kWh", context: "Transaction.Begin"));
        // 6 kWh as 6 x 10^3 Wh; the per-phase reading and the power sample do not replace the total.
        await EventWithSamples(host, "Updated", "MeterValuePeriodic", T0.AddMinutes(10), 1,
            Sample(6, "Wh", multiplier: 3), Sample(2100, "Wh", phase: "L1"), Sample(7.4, "kW", measurand: "Power.Active.Import"));
        Assert.Equal(6.0, (await Tx(host)).MeterStop!.Value, 6);
        // 9 kWh as 9 000 000 x 10^-3 Wh, without unit (Wh is the default).
        await EventWithSamples(host, "Updated", "MeterValuePeriodic", T0.AddMinutes(20), 2,
            new { value = 9_000_000, measurand = "Energy.Active.Import.Register", unitOfMeasure = new { multiplier = -3 } });
        Assert.Equal(9.0, (await Tx(host)).MeterStop!.Value, 6);
        // The end: a periodic sample and the Transaction.End reading in the same message; the latter is the final register.
        await EventWithSamples(host, "Ended", "StopAuthorized", T0.AddMinutes(30), 3,
            Sample(12.9, "kWh", context: "Sample.Periodic"), Sample(13, "kWh", context: "Transaction.End"));

        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
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
