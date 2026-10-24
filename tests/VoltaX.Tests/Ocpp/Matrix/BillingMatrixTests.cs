using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp.Matrix;

/// <summary>Money around sessions, for both protocols: low-balance stop, idle fee, the card that pays.</summary>
public class BillingMatrixTests
{
    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task The_charger_is_told_to_stop_when_the_running_cost_reaches_the_balance(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol, balance: 10m);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 0, T0);

        await charger.Meter(uid, 2000, T0.AddMinutes(5)); // 5.00 + 0.50 + 1.00 = 6.50, still covered
        Assert.DoesNotContain(host.Sender.Sent, s => s.Action is "RequestStopTransaction" or "RemoteStopTransaction");
        await charger.Meter(uid, 4000, T0.AddMinutes(10)); // 10.00 + 1.00 + 1.00 = 12.00 > 10.00

        var stopAction = protocol == OcppTestHost.Ocpp16 ? "RemoteStopTransaction" : "RequestStopTransaction";
        Assert.True(await host.Sender.WaitFor(stopAction));
        var stop = host.Sender.Sent.First(s => s.Action == stopAction).Payload;
        Assert.Contains(protocol == OcppTestHost.Ocpp16 ? $"\"transactionId\":{ScenarioCharger16.TransactionId(uid)}" : uid, stop);
        Assert.Contains("LowBalance", await ScenarioAssert.Notifications(host));

        // The charger obeys a little later: the energy delivered meanwhile is billed, the card goes negative.
        await charger.Stop(uid, 4200, T0.AddMinutes(11), StopKind.Remote);
        await ScenarioAssert.Billed(host, cardId, 10m, energy: 10.50m, time: 1.10m, flatFee: 1.00m);
        Assert.Equal(10m - 12.60m, await ScenarioAssert.Balance(host, cardId));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Idle_time_after_the_grace_period_is_billed_when_the_car_leaves(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");
        Assert.Equal(ChargingSessionStatusEnum.CompletedOccupied, (await Session(host)).ChargingSessionStatus);

        await charger.Unplug(T0.AddMinutes(50));

        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m, idle: 10.00m);
        Assert.Equal(20, session.IdleMinutes!.Value, 6);
        Assert.Equal(ChargingSessionStatusEnum.Completed, session.ChargingSessionStatus);
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task An_old_session_never_gets_the_idle_time_of_a_later_plug_in(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");

        // The charger reboots before the car leaves: the connector goes Unavailable, then Available.
        await charger.Unavailable(T0.AddMinutes(33));
        await charger.Unplug(T0.AddMinutes(35));
        // The next day someone plugs in and leaves without charging.
        await charger.Plug(T0.AddDays(1));
        await charger.Unplug(T0.AddDays(1).AddMinutes(5));

        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(0m, session.IdleAmount);
        Assert.Equal(3, session.IdleMinutes!.Value, 6);
        Assert.Equal(ChargingSessionStatusEnum.Completed, session.ChargingSessionStatus);
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Idle_time_is_billed_to_the_latest_session_only(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, first) = await charger.Start("TAG-1", 1000, T0);
        await charger.Stop(first, 5000, T0.AddMinutes(10), StopKind.LocalCard, idTag: "TAG-1");
        // The car left but the Available status was lost; the next driver plugs in and charges.
        await charger.Plug(T0.AddHours(2));
        var (_, second) = await charger.Start("TAG-2", 5000, T0.AddHours(2));
        await charger.Fault(T0.AddHours(2).AddMinutes(5));
        await charger.Recover(T0.AddHours(2).AddMinutes(6));
        await charger.Stop(second, 9000, T0.AddHours(2).AddMinutes(20), StopKind.LocalCard, idTag: "TAG-2");
        await charger.Unplug(T0.AddHours(2).AddMinutes(22));

        var sessions = await host.Query(db => db.ChargingSessions.OrderBy(s => s.ID).ToListAsync());
        Assert.All(sessions, s => Assert.Equal(0m, s.IdleAmount ?? 0m));
        Assert.False(await host.Query(db => db.WalletTransactions.AnyAsync(w => w.Type == WalletTransactionType.IdleFee)));
        Assert.Equal(100m - 10.00m - 1.00m - 1.00m, await ScenarioAssert.Balance(host, cardId));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_session_stopped_with_another_card_is_billed_to_the_card_that_started_it(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);

        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-2");

        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(50m, await host.Query(db => db.Cards.Where(c => c.CardNumber == "TAG-2").Select(c => c.Balance).SingleAsync()));
        Assert.Contains("ChargingStopCardMismatch", await ScenarioAssert.Notifications(host));
    }
}
