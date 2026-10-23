using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Ocpp.Matrix;

/// <summary>
/// The station every scenario of the matrix runs on, once per protocol (OCPP 1.6J and 2.0.1): charge point CP-MATRIX,
/// EVSE 1 / connector 1 priced 2.50/kWh, 0.10/min, 1.00 per session and 0.50 per idle minute after 5 minutes of grace,
/// VAT 20 %. The driver's card TAG-1 holds 100.00 (unless stated); TAG-2 belongs to someone else; BLOCKED, EXPIRED and
/// EMPTY (no balance) are the driver's refused cards.
///
/// The reference session, 1 kWh to 13 kWh in 30 minutes, costs 30.00 + 3.00 + 1.00 = 34.00.
/// </summary>
public static class MatrixStation
{
    public const string Cp = "CP-MATRIX";
    public static readonly DateTime T0 = Seeds.Now;

    public static TheoryData<string> Protocols => new() { OcppTestHost.Ocpp16, OcppTestHost.Ocpp201 };

    /// <summary>The station, booted and Accepted; returns the host, the charger and TAG-1's card id.</summary>
    public static async Task<(OcppTestHost Host, ScenarioCharger Charger, int CardId)> Open(string protocol, decimal balance = 100m,
        Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>? configure = null)
    {
        var host = new OcppTestHost(configure: configure, relational: true);
        var cardId = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c =>
            {
                c.PricePerKWh = 2.50m;
                c.PricePerMinute = 0.10m;
                c.FlatFee = 1.00m;
                c.PricePerIdleMinute = 0.50m;
            });
            var driver = await Seeds.User(db);
            var other = await Seeds.User(db, "other@example.com");
            var card = Seeds.Card("TAG-1", balance, driver.ID);
            db.Cards.AddRange(card,
                Seeds.Card("TAG-2", 50m, other.ID),
                Seeds.Card("BLOCKED", 50m, driver.ID, blocked: true),
                Seeds.Card("EXPIRED", 50m, driver.ID, expires: T0.AddYears(-1)),
                Seeds.Card("EMPTY", 0m, driver.ID));
            await db.SaveChangesAsync();
            cardId = card.ID;
        });
        var charger = ScenarioCharger.For(host, protocol, Cp);
        Assert.Equal("Accepted", await charger.Boot());
        return (host, charger, cardId);
    }

    public static Task<Transaction> Tx(OcppTestHost host) => host.Query(db => db.Transactions.SingleAsync());

    public static Task<ChargingSession> Session(OcppTestHost host) => host.Query(db => db.ChargingSessions.SingleAsync());
}
