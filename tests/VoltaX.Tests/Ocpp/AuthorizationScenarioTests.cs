using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Ocpp;

/// <summary>Who may charge: tokens, balances and charger provisioning, for both protocol versions.</summary>
public class AuthorizationScenarioTests
{
    private const string Cp = "CP-AUTH";
    private static readonly DateTime T0 = Seeds.Now;

    private static async Task<OcppTestHost> Station()
    {
        var host = new OcppTestHost(relational: true);
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c => c.PricePerKWh = 2m);
            db.ChargePoints.Add(new ChargePoint { ChargePointId = "CP-NEW", ChargingStationID = (await Seeds.Station(db, name: "New")).ID });
            var user = await Seeds.User(db);
            db.Cards.AddRange(
                Seeds.Card("OK", 50m, user.ID),
                Seeds.Card("BLOCKED", 50m, user.ID, blocked: true),
                Seeds.Card("EXPIRED", 50m, user.ID, expires: T0.AddDays(-1).AddYears(-1)),
                Seeds.Card("EMPTY", 0m, user.ID));
        });
        return host;
    }

    [Theory]
    [InlineData("OK", "Accepted")]
    [InlineData("BLOCKED", "Blocked")]
    [InlineData("EXPIRED", "Expired")]
    [InlineData("NOPE", "Invalid")]
    public async Task Authorize_2_0_1(string token, string expected)
    {
        await using var host = await Station();
        var charger = new Charger201(host, Cp);
        await charger.Boot();

        Assert.Equal(expected, await charger.Authorize(token));
    }

    [Theory]
    [InlineData("BLOCKED", "Blocked")]
    [InlineData("EXPIRED", "Expired")]
    [InlineData("NOPE", "Invalid")]
    [InlineData("EMPTY", "NoCredit")]
    public async Task Refused_tokens_never_open_a_session_2_0_1(string token, string expected)
    {
        await using var host = await Station();
        var charger = new Charger201(host, Cp);
        await charger.Boot();

        Assert.Equal(expected, await charger.Started("TX-R", token, 0, T0));

        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));
        Assert.False(await host.Query(db => db.Transactions.AnyAsync()));
    }

    [Theory]
    [InlineData("BLOCKED", "Blocked")]
    [InlineData("EMPTY", "Invalid")] // 1.6 has no NoCredit
    [InlineData("NOPE", "Invalid")]
    public async Task Refused_tokens_never_open_a_session_1_6(string token, string expected)
    {
        await using var host = await Station();
        var charger = new Charger16(host, Cp);
        await charger.Boot();

        var (status, _) = await charger.Start(token, 0, T0);

        Assert.Equal(expected, status);
        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));
    }

    [Fact]
    public async Task A_charger_waiting_for_provisioning_cannot_charge()
    {
        await using var host = await Station();
        var charger = new Charger16(host, "CP-NEW");

        Assert.Equal("Pending", (await charger.Boot()).GetProperty("status").GetString());
        Assert.Equal("Invalid", await charger.Authorize("OK"));
        Assert.Equal("Invalid", (await charger.Start("OK", 0, T0)).Status);
    }

    [Fact]
    public async Task Refusals_tell_the_card_owner_why()
    {
        await using var host = await Station();
        var charger = new Charger201(host, Cp);
        await charger.Boot();

        await charger.Started("TX-1", "EMPTY", 0, T0);
        await charger.Started("TX-2", "BLOCKED", 0, T0);

        var actions = await host.Query(db => db.Notifications.Select(n => n.Action).ToListAsync());
        Assert.Contains("ChargingStartRejectedNoCredit", actions);
        Assert.Contains("ChargingStartRejectedBlockedCard", actions);
    }
}
