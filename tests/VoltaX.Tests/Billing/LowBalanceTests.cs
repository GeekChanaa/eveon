using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;

namespace VoltaX.Tests.Billing;

/// <summary>
/// Low balance, for both protocol versions: no start below the minimum balance, the running cost watched on every
/// meter reading, a remote stop when what is left reaches the buffer, and the customer told.
/// </summary>
public class LowBalanceTests
{
    private const string Cp = "CP-LOW";
    private static readonly DateTime T0 = Seeds.Now;

    /// <summary>2.50/kWh, no time price; minimum balance 20 MAD to start, stop when 5 MAD or less would be left.</summary>
    private static async Task<OcppTestHost> Station(string protocol)
    {
        var host = new OcppTestHost(new Dictionary<string, string?>
        {
            ["GlobalConfigurations:DefaultMinimumRequiredBalance"] = "20",
            ["GlobalConfigurations:DefaultLowBalanceThresholdBuffer"] = "5"
        }, relational: true);
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c => c.PricePerKWh = 2.50m);
            var user = await Seeds.User(db);
            db.Cards.AddRange(Seeds.Card("RICH", 30m, user.ID), Seeds.Card("POOR", 19.99m, user.ID));
        });
        host.Sender.Versions[Cp] = protocol;
        return host;
    }

    [Fact]
    public async Task Ocpp201_refuses_to_start_below_the_minimum_balance_and_tells_the_customer()
    {
        await using var host = await Station(OcppTestHost.Ocpp201);
        var charger = new Charger201(host, Cp);
        await charger.Boot();

        Assert.Equal("NoCredit", await charger.Authorize("POOR"));
        Assert.Equal("Accepted", await charger.Authorize("RICH"));
        Assert.Equal("NoCredit", await charger.Started("TX-1", "POOR", 0, T0));

        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));
        var notice = await host.Query(db => db.Notifications.SingleAsync(n => n.Action == "ChargingStartRejectedNoCredit"));
        Assert.Contains("19.99", notice.Description);
        Assert.Contains("20.00", notice.Description);
    }

    [Fact]
    public async Task Ocpp16_refuses_to_start_below_the_minimum_balance()
    {
        await using var host = await Station(OcppTestHost.Ocpp16);
        var charger = new Charger16(host, Cp);
        await charger.Boot();

        Assert.Equal("Invalid", await charger.Authorize("POOR")); // 1.6 has no NoCredit
        var (status, _) = await charger.Start("POOR", 0, T0);

        Assert.Equal("Invalid", status);
        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));
    }

    [Fact]
    public async Task Ocpp16_meter_values_stop_the_session_remotely_when_the_balance_runs_low()
    {
        await using var host = await Station(OcppTestHost.Ocpp16);
        var charger = new Charger16(host, Cp);
        await charger.Boot();
        await charger.Status(1, "Preparing", at: T0);
        var (status, transactionId) = await charger.Start("RICH", 0, T0);
        Assert.Equal("Accepted", status);

        await charger.Meter(transactionId, 5000, T0.AddMinutes(10)); // 12.50 + 0 flat: 17.50 left
        Assert.DoesNotContain(host.Sender.Sent, s => s.Action == "RemoteStopTransaction");
        await charger.Meter(transactionId, 10000, T0.AddMinutes(20)); // 25.00: 5.00 left, at the buffer

        Assert.True(await host.Sender.WaitFor("RemoteStopTransaction"));
        Assert.Contains($"\"transactionId\":{transactionId}", host.Sender.Sent.First(s => s.Action == "RemoteStopTransaction").Payload);
        var notice = await host.Query(db => db.Notifications.SingleAsync(n => n.Action == "LowBalance"));
        Assert.True(notice.Urgent);
        Assert.Contains("5.00", notice.Description);
    }

    [Fact]
    public async Task Ocpp201_stops_before_the_balance_is_exhausted_when_a_buffer_is_set()
    {
        await using var host = await Station(OcppTestHost.Ocpp201);
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Started("TX-2", "RICH", 0, T0);

        await charger.Updated("TX-2", 9000, T0.AddMinutes(15)); // 22.50: 7.50 left
        Assert.DoesNotContain(host.Sender.Sent, s => s.Action == "RequestStopTransaction");
        await charger.Updated("TX-2", 10200, T0.AddMinutes(17)); // 25.50: 4.50 left, under the 5.00 buffer

        Assert.True(await host.Sender.WaitFor("RequestStopTransaction"));
        Assert.True(await host.Query(db => db.Notifications.AnyAsync(n => n.Action == "LowBalance")));
        // The card is not debited before the transaction ends.
        Assert.Equal(30m, await host.Query(db => db.Cards.Where(c => c.CardNumber == "RICH").Select(c => c.Balance).SingleAsync()));
    }
}
