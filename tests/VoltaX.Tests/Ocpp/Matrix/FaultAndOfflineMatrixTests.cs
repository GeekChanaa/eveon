using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp.Matrix;

/// <summary>Faults, power cuts, offline periods, reboots, reconnections and chargers that may not charge, for both protocols.</summary>
public class FaultAndOfflineMatrixTests
{
    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_transaction_that_happened_entirely_offline_is_billed_when_it_is_reported(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;

        // Card accepted from the charger's local list while the link was down; everything arrives in one burst.
        var (status, uid) = await charger.Start("TAG-1", 1000, T0, offline: true);
        await charger.Meter(uid, 7000, T0.AddMinutes(15), offline: true);
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.EVDisconnected, offline: true);

        Assert.Equal("Accepted", status);
        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(T0, session.StartDate);
        Assert.Equal(T0.AddMinutes(30), session.EndDate);
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_charger_that_reboots_mid_session_and_resumes_keeps_one_session(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(uid, 5000, T0.AddMinutes(10));

        // Reboot (watchdog): the charger keeps the transaction in memory and carries on with it.
        Assert.Equal("Accepted", await charger.Boot());
        await charger.Resume(uid, 5000, T0.AddMinutes(12));
        await charger.Meter(uid, 9000, T0.AddMinutes(20));

        Assert.Equal(TransactionStatusEnum.Current, (await Tx(host)).Status);
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");
        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.DoesNotContain("ChargingEndedAbnormally", await ScenarioAssert.Notifications(host));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_charger_that_reconnects_on_a_new_connection_continues_its_session(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(uid, 5000, T0.AddMinutes(10));

        // Network drop: a new WebSocket, no BootNotification (the charger did not restart).
        host.Reconnect(Cp, protocol);
        await charger.Meter(uid, 9000, T0.AddMinutes(20));
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");

        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Unknown_and_pending_chargers_cannot_charge(string protocol)
    {
        await using var host = new OcppTestHost(relational: true);
        await host.Seed(async db =>
        {
            db.ChargePoints.Add(new ChargePoint { ChargePointId = "CP-PENDING", ChargingStationID = (await Seeds.Station(db)).ID });
            var user = await Seeds.User(db);
            db.Cards.Add(Seeds.Card("TAG-1", 100m, user.ID));
        });
        var unknown = ScenarioCharger.For(host, protocol, "CP-UNKNOWN");
        var pending = ScenarioCharger.For(host, protocol, "CP-PENDING");

        Assert.Equal("Rejected", await unknown.Boot());
        Assert.Equal("Pending", await pending.Boot());
        foreach (var charger in new[] { unknown, pending })
        {
            Assert.Equal("Invalid", await charger.Authorize("TAG-1"));
            var (status, uid) = await charger.Start("TAG-1", 1000, T0);
            Assert.Equal("Invalid", status);
            await charger.Stop(uid, 5000, T0.AddMinutes(10), StopKind.DeAuthorized);
        }

        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));
        Assert.False(await host.Query(db => db.Transactions.AnyAsync()));
        Assert.Equal(100m, await host.Query(db => db.Cards.Select(c => c.Balance).SingleAsync()));
    }
}
