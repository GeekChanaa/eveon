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
}
