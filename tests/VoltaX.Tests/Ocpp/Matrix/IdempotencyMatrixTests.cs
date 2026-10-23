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
}
