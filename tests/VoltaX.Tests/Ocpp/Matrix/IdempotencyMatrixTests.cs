using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp.Matrix;

/// <summary>Messages sent twice or out of order (lost answers, retries, offline queues), for both protocols.</summary>
public class IdempotencyMatrixTests
{
    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Retried_and_duplicated_messages_are_applied_once(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);

        // Each transaction message is sent twice, unchanged (its CALLRESULT was lost).
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        var (statusAgain, uidAgain) = charger.ReadStart(await charger.RepeatLast(), uid);
        await charger.Meter(uid, 6000, T0.AddMinutes(10));
        await charger.RepeatLast();
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");
        await charger.RepeatLast();

        Assert.Equal("Accepted", statusAgain);
        Assert.Equal(uid, uidAgain);
        Assert.Equal(1, await host.Query(db => db.Transactions.CountAsync()));
        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.DoesNotContain("ChargingEndedAbnormally", await ScenarioAssert.Notifications(host));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_meter_value_delivered_late_never_moves_the_session_backwards(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(uid, 5000, T0.AddMinutes(10));
        await charger.Meter(uid, 9000, T0.AddMinutes(30));

        // The 20-minute sample timed out and is retried after the 30-minute one.
        await charger.Meter(uid, 7000, T0.AddMinutes(20));

        Assert.Equal(9.0, (await Tx(host)).MeterStop);
        var session = await Session(host);
        Assert.Equal(8, session.ChargedKwhs!.Value, 6);
        Assert.Equal(30, session.ChargedMinutes!.Value, 6);

        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");
        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
    }
}
