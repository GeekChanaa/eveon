using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Models.Alarms;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;
using VoltaXApi.Services.Alarms;

namespace VoltaX.Tests.Ocpp;

/// <summary>Alarms raised, counted and cleared from what OCPP 1.6 and 2.0.1 chargers report.</summary>
public class AlarmScenarioTests
{
    private const string Cp = "CP-ALARM";
    private static readonly DateTime T0 = Seeds.Now;

    private static async Task<OcppTestHost> Host(string protocol = OcppTestHost.Ocpp201, IDictionary<string, string?>? settings = null)
    {
        var values = new Dictionary<string, string?> { ["Alarms:EmailRecipients"] = "noc@eveon.test" };
        if (settings != null) foreach (var (k, v) in settings) values[k] = v;
        var host = new OcppTestHost(values, relational: true);
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c => { c.PricePerKWh = 2m; });
            await Seeds.User(db, "admin@eveon.test", role: "Admin");
        });
        host.Sender.Versions[Cp] = protocol;
        return host;
    }

    private static Task<List<Alarm>> Alarms(OcppTestHost host) => host.Query(db => db.Alarms.AsNoTracking().OrderBy(a => a.ID).ToListAsync());

    private static async Task<bool> Eventually(Func<bool> condition)
    {
        var until = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < until)
        {
            if (condition()) return true;
            await Task.Delay(20);
        }
        return condition();
    }

    [Fact]
    public async Task A_16_fault_is_raised_once_counted_while_it_lasts_and_cleared_when_the_connector_recovers()
    {
        await using var host = await Host(OcppTestHost.Ocpp16);
        var charger = new Charger16(host, Cp);
        await charger.Boot();
        await charger.Status(1, "Available", at: T0);

        await charger.Call("StatusNotification", new { connectorId = 1, errorCode = "GroundFailure", status = "Faulted", info = "RCD tripped", vendorErrorCode = "E42", timestamp = T0.AddMinutes(1) });
        await charger.Status(1, "Faulted", "GroundFailure", T0.AddMinutes(2));

        var alarm = Assert.Single(await Alarms(host));
        Assert.Equal(AlarmType.ConnectorFault, alarm.Type);
        Assert.Equal("GroundFailure", alarm.Code);
        Assert.Equal(AlarmSeverity.Critical, alarm.Severity);
        Assert.Equal(AlarmStatus.Open, alarm.Status);
        Assert.Equal(2, alarm.Count);
        Assert.Equal(1, alarm.EvseId);
        Assert.Equal(T0.AddMinutes(1), alarm.FirstSeenAt);
        Assert.Equal(T0.AddMinutes(2), alarm.LastSeenAt);
        Assert.Contains("RCD tripped", alarm.Details);
        Assert.NotNull(alarm.ConnectorID);

        await charger.Status(1, "Available", at: T0.AddMinutes(10));

        alarm = Assert.Single(await Alarms(host));
        Assert.Equal(AlarmStatus.Cleared, alarm.Status);
        Assert.Equal(T0.AddMinutes(10), alarm.ClearedAt);

        // The same fault later is a new alarm.
        await charger.Status(1, "Faulted", "GroundFailure", T0.AddMinutes(20));
        Assert.Equal(2, (await Alarms(host)).Count(a => a.Code == "GroundFailure"));
    }

    [Fact]
    public async Task A_16_error_without_fault_is_minor_and_the_charger_level_is_kept_apart()
    {
        await using var host = await Host(OcppTestHost.Ocpp16);
        var charger = new Charger16(host, Cp);
        await charger.Boot();

        await charger.Status(1, "Available", "ReaderFailure", T0);
        await charger.Status(0, "Available", "HighTemperature", T0);
        await charger.Status(0, "Available", at: T0.AddMinutes(1));

        var alarms = await Alarms(host);
        var reader = alarms.Single(a => a.Code == "ReaderFailure");
        var temperature = alarms.Single(a => a.Code == "HighTemperature");
        Assert.Equal(AlarmSeverity.Minor, reader.Severity);
        Assert.Equal(AlarmStatus.Open, reader.Status);
        Assert.Equal(AlarmSeverity.Major, temperature.Severity);
        Assert.Null(temperature.EvseId);
        Assert.Equal(AlarmStatus.Cleared, temperature.Status);
    }

    [Fact]
    public async Task A_201_faulted_connector_raises_a_critical_alarm_that_notifies_and_emails_and_clears_on_recovery()
    {
        await using var host = await Host();
        var charger = new Charger201(host, Cp);
        await charger.Boot();

        await charger.Status("Faulted", T0, evseId: 1, connectorId: 1);
        await charger.Status("Faulted", T0.AddMinutes(1), evseId: 1, connectorId: 1);

        var alarm = Assert.Single(await Alarms(host));
        Assert.Equal(AlarmSeverity.Critical, alarm.Severity);
        Assert.Equal("Faulted", alarm.Code);
        Assert.Equal(2, alarm.Count);
        Assert.Equal((1, 1), (alarm.EvseId!.Value, alarm.ConnectorNumber!.Value));

        // Notified once (a new critical alarm), in the background.
        Assert.True(await Eventually(() => host.Mail.CallsTo(nameof(IMailService.SendAlarmEmail)).Count() == 1));
        var mail = host.Mail.CallsTo(nameof(IMailService.SendAlarmEmail)).Single();
        Assert.Contains("noc@eveon.test", ((MailRequest)mail[0]!).ToEmails);
        Assert.Equal(alarm.ID, ((AlarmMail)mail[1]!).Id);
        Assert.True(await host.Query(db => db.Notifications.AnyAsync(n => n.Action == "CriticalAlarm" && n.Urgent)));

        await charger.Status("Available", T0.AddMinutes(5), evseId: 1, connectorId: 1);
        Assert.Equal(AlarmStatus.Cleared, Assert.Single(await Alarms(host)).Status);
    }

    [Fact]
    public async Task A_201_unavailable_connector_is_not_an_alarm()
    {
        await using var host = await Host();
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Unavailable", T0);
        Assert.Empty(await Alarms(host));
    }

    [Fact]
    public async Task A_201_problem_event_raises_an_alarm_and_its_cleared_event_closes_it()
    {
        await using var host = await Host();
        var charger = new Charger201(host, Cp);
        await charger.Boot();

        object Event(int id, string value, bool? cleared, DateTime at) => new
        {
            generatedAt = at.ToString("o"),
            seqNo = 0,
            tbc = false,
            eventData = new[]
            {
                new
                {
                    eventId = id, timestamp = at.ToString("o"), trigger = "Delta", actualValue = value, cleared, techCode = "OVT",
                    eventNotificationType = "HardWiredNotification",
                    component = new { name = "Connector", evse = new { id = 1, connectorId = 1 } },
                    variable = new { name = "Problem" }
                }
            }
        };

        await charger.Call("NotifyEvent", Event(1, "true", null, T0));
        var alarm = Assert.Single(await Alarms(host));
        Assert.Equal(AlarmType.DeviceEvent, alarm.Type);
        Assert.Equal("Connector.Problem", alarm.Code);
        Assert.Equal(AlarmSeverity.Major, alarm.Severity);
        Assert.Contains("OVT", alarm.Details);

        await charger.Call("NotifyEvent", Event(2, "false", true, T0.AddMinutes(3)));
        alarm = Assert.Single(await Alarms(host));
        Assert.Equal(AlarmStatus.Cleared, alarm.Status);
    }

    [Fact]
    public async Task Repeated_accepted_boots_raise_a_boot_loop_alarm_but_pending_boots_do_not()
    {
        await using var host = await Host(OcppTestHost.Ocpp16);
        await host.Seed(db => { db.ChargePoints.Add(new ChargePoint { ChargePointId = "CP-NEW", ChargingStationID = db.ChargingStations.First().ID }); return Task.CompletedTask; });
        var charger = new Charger16(host, Cp);
        var pending = new Charger16(host, "CP-NEW");

        for (var i = 0; i < 4; i++) await pending.Boot();
        await charger.Boot();
        await charger.Boot();
        Assert.Empty(await Alarms(host));

        await charger.Boot();
        await charger.Boot();

        var alarm = Assert.Single(await Alarms(host));
        Assert.Equal(AlarmType.BootLoop, alarm.Type);
        Assert.Equal(AlarmSeverity.Major, alarm.Severity);
        Assert.Equal(2, alarm.Count);
    }

    [Fact]
    public async Task A_charger_offline_longer_than_the_delay_raises_one_alarm_cleared_when_it_reconnects()
    {
        await using var host = await Host(OcppTestHost.Ocpp16, new Dictionary<string, string?> { ["Alarms:OfflineAfterMinutes"] = "15" });
        var now = DateTime.UtcNow;
        await host.Seed(async db =>
        {
            var cp = await db.ChargePoints.SingleAsync(c => c.ChargePointId == Cp);
            db.ChargePointUptimes.Add(new ChargePointUptime { ChargePointID = cp.ID, StartDate = now.AddMinutes(-30), ChargePointUptimeStatus = ChargePointUptimeStatusEnum.Available });
            // A charger never seen online is not "offline".
            await Seeds.ChargePoint(db, "CP-NEVER", chargingStationId: cp.ChargingStationID);
        });
        var monitor = host.Provider.GetRequiredService<ChargerAvailabilityMonitor>();

        Assert.Equal(0, await monitor.CheckAsync(now.AddMinutes(5)));
        Assert.Equal(1, await monitor.CheckAsync(now.AddMinutes(20)));
        Assert.Equal(0, await monitor.CheckAsync(now.AddMinutes(25)));

        var alarm = Assert.Single(await Alarms(host));
        Assert.Equal(AlarmType.Offline, alarm.Type);
        Assert.Equal(AlarmSeverity.Critical, alarm.Severity);
        Assert.Equal(1, alarm.Count);

        host.Provider.GetRequiredService<WebSocketManagerService>().AddConnection(host.Connection(Cp, OcppTestHost.Ocpp16));
        Assert.Equal(1, await monitor.CheckAsync(now.AddMinutes(26)));
        Assert.Equal(AlarmStatus.Cleared, Assert.Single(await Alarms(host)).Status);
    }

    [Fact]
    public async Task A_boot_loop_alarm_clears_once_the_charger_stays_up()
    {
        await using var host = await Host(OcppTestHost.Ocpp16, new Dictionary<string, string?> { ["Alarms:BootLoopWindowMinutes"] = "10" });
        var charger = new Charger16(host, Cp);
        for (var i = 0; i < 3; i++) await charger.Boot();
        Assert.Equal(AlarmType.BootLoop, Assert.Single(await Alarms(host)).Type);

        host.Provider.GetRequiredService<WebSocketManagerService>().AddConnection(host.Connection(Cp, OcppTestHost.Ocpp16));
        var monitor = host.Provider.GetRequiredService<ChargerAvailabilityMonitor>();
        await host.Seed(async db => db.ChargePointUptimes.Add(new ChargePointUptime { ChargePointID = (await db.ChargePoints.SingleAsync(c => c.ChargePointId == Cp)).ID, StartDate = DateTime.UtcNow }));

        Assert.Equal(0, await monitor.CheckAsync(DateTime.UtcNow.AddMinutes(5)));
        Assert.Equal(1, await monitor.CheckAsync(DateTime.UtcNow.AddMinutes(21)));
        Assert.Equal(AlarmStatus.Cleared, Assert.Single(await Alarms(host)).Status);
    }

    [Fact]
    public async Task Faults_of_unknown_chargers_are_ignored()
    {
        await using var host = await Host(OcppTestHost.Ocpp16);
        await host.With<IAlarmService>(alarms => alarms.OnStatus16Async("NOPE", 1, "Faulted", "GroundFailure", null, null, T0));
        Assert.Empty(await Alarms(host));
    }

    [Fact]
    public async Task An_alarm_resolved_by_an_operator_is_not_reopened_and_the_fault_reported_again_is_a_new_one()
    {
        await using var host = await Host();
        var charger = new Charger201(host, Cp);
        await charger.Boot();
        await charger.Status("Faulted", T0);
        var first = Assert.Single(await Alarms(host));
        var admin = await host.Query(db => db.Users.SingleAsync(u => u.Email == "admin@eveon.test"));

        var resolved = await host.With<IAlarmService, AlarmActionResult>(a => a.ResolveAsync(first.ID, admin.ID, "Replaced the contactor"));
        Assert.Equal(AlarmActionOutcome.Done, resolved.Outcome);
        await charger.Status("Available", T0.AddMinutes(1));
        await charger.Status("Faulted", T0.AddMinutes(2));

        var alarms = await Alarms(host);
        Assert.Equal(2, alarms.Count);
        Assert.Equal(AlarmStatus.Resolved, alarms[0].Status);
        Assert.Equal("Replaced the contactor", alarms[0].ResolutionComment);
        Assert.Equal(AlarmStatus.Open, alarms[1].Status);
    }
}
