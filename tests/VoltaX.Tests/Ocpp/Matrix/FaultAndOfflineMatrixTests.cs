using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp.Matrix;

/// <summary>Faults, power cuts, offline periods, reboots, reconnections and chargers that may not charge, for both protocols.</summary>
public class FaultAndOfflineMatrixTests
{
    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_fault_mid_session_bills_only_the_measured_energy_and_no_idle_time(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(uid, 6000, T0.AddMinutes(10));

        await charger.Fault(T0.AddMinutes(11));
        Assert.Equal(ConnectorStatusEnumType.Faulted, await ScenarioAssert.ConnectorStatus(host, Cp));
        Assert.Equal(TransactionStatusEnum.Current, (await Tx(host)).Status);
        await charger.Stop(uid, 7000, T0.AddMinutes(12), StopKind.Fault);
        // The fault clears; the driver comes back and unplugs much later.
        await charger.Recover(T0.AddMinutes(20));
        await charger.Unplug(T0.AddMinutes(90));

        var session = await ScenarioAssert.Billed(host, cardId, 100m, energy: 15.00m, time: 1.20m, flatFee: 1.00m);
        Assert.Equal(charger.StoredReason(StopKind.Fault), session.StoppedReason);
        Assert.Equal(ChargingSessionStatusEnum.Completed, session.ChargingSessionStatus);
        var notifications = await ScenarioAssert.Notifications(host);
        Assert.Contains("ChargerFaultDuringSession", notifications);
        Assert.Contains("ChargingEndedAbnormally", notifications);
        Assert.Single(host.SystemReports.CallsTo(nameof(VoltaXApi.Services.ISystemReportService.HandleReport)));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_power_loss_reported_after_the_reboot_is_billed_on_charger_time(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(uid, 6000, T0.AddMinutes(10));

        // Power cut at T0+15. The charger comes back an hour later and sends what it queued, with past timestamps.
        Assert.Equal("Accepted", await charger.Boot());
        await charger.Meter(uid, 9000, T0.AddMinutes(14), offline: true);
        await charger.Stop(uid, 9500, T0.AddMinutes(15), StopKind.PowerLoss, offline: true);
        await charger.Unplug(T0.AddMinutes(75));

        // 8.5 kWh = 21.25, 15 minutes = 1.50, flat fee 1.00; no idle fee for the outage.
        var session = await ScenarioAssert.Billed(host, cardId, 100m, energy: 21.25m, time: 1.50m, flatFee: 1.00m);
        Assert.Equal(ReasonEnumType.PowerLoss, session.StoppedReason);
        Assert.Equal(T0.AddMinutes(15), (await Tx(host)).StopTime);
        Assert.Contains("ChargingEndedAbnormally", await ScenarioAssert.Notifications(host));
        Assert.Equal(ConnectorStatusEnumType.Available, await ScenarioAssert.ConnectorStatus(host, Cp));
    }

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

    [Fact]
    public void The_old_connection_of_a_reconnected_charger_closing_late_does_not_unregister_the_new_one()
    {
        var registry = new VoltaXApi.OCPP.Services.WebSocketManagerService();
        VoltaXApi.OCPP.Models.OcppConnection Connect() => new(Cp, OcppTestHost.Ocpp201, new IdleWebSocket(OcppTestHost.Ocpp201),
            new OCPP.Core.Server.ChargePointStatus { Id = Cp, Protocol = OcppTestHost.Ocpp201 });
        var first = Connect();
        var second = Connect();

        Assert.Null(registry.AddConnection(first));
        Assert.Same(first, registry.AddConnection(second));   // the handler aborts the previous connection
        Assert.False(registry.RemoveConnection(first));        // its receive loop ends afterwards

        Assert.Same(second, registry.GetConnection(Cp));
        Assert.True(registry.RemoveConnection(second));
        Assert.Null(registry.GetConnection(Cp));
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
