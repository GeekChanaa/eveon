using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Hubs;
using VoltaXApi.Services;
using VoltaXApi.Services.Alarms;

namespace VoltaX.Tests.Ocpp;

/// <summary>What the dashboard receives on the charger hub: connection status of a charger, alarm changes.</summary>
public class RealtimePushTests
{
    private static async Task<(OcppTestHost Host, RecordingHubContext<ChargerHub> Hub)> Host()
    {
        var hub = new RecordingHubContext<ChargerHub>();
        var host = new OcppTestHost(configure: services => services.AddSingleton<IHubContext<ChargerHub>>(hub), relational: true);
        await host.Seed(db => Seeds.ChargePoint(db, "CP-LIVE", c => { }));
        return (host, hub);
    }

    private static JsonElement Json(object? payload) => JsonSerializer.SerializeToElement(payload);

    [Fact]
    public async Task Pages_watching_a_charger_get_its_connection_status()
    {
        var (host, hub) = await Host();
        await using var _ = host;
        var notifier = host.Provider.GetRequiredService<ChargePointConnectivityNotifier>();

        notifier.Connected("CP-LIVE");
        notifier.Disconnected("CP-LIVE");

        var statuses = hub.SentTo("CP-LIVE", ChargePointConnectivityNotifier.ConnectionEvent).Select(p => Json(p).GetProperty("isActive").GetBoolean()).ToList();
        Assert.Equal(new[] { true, false }, statuses);
    }

    [Fact]
    public async Task Alarm_changes_are_pushed_to_the_alarm_feed()
    {
        var (host, hub) = await Host();
        await using var _ = host;
        var charger = new Charger201(host, "CP-LIVE");
        await charger.Boot();

        await charger.Status("Faulted", Seeds.Now);
        await charger.Status("Faulted", Seeds.Now.AddMinutes(1));
        await charger.Status("Available", Seeds.Now.AddMinutes(2));

        var changes = hub.SentTo(AlarmNotifier.HubGroup, AlarmNotifier.HubMethod).Select(Json).ToList();
        Assert.Equal(new[] { "Raised", "Updated", "Cleared" }, changes.Select(c => c.GetProperty("change").GetString()));
        var alarm = changes[0].GetProperty("alarm");
        Assert.Equal("CP-LIVE", alarm.GetProperty("ChargePointIdentity").GetString());
        Assert.Equal("Critical", alarm.GetProperty("Severity").GetString());
    }
}
