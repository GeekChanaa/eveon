using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Hubs;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.Services;

namespace VoltaX.Tests.CustomerPortal;

/// <summary>
/// The customer portal's live session widget: the owner's connections receive the session's updates (energy, power,
/// cost) and the owner, and only the owner, can stop it.
/// </summary>
public class LiveSessionTests
{
    private const string Cp = "CP-LIVE";
    private static readonly DateTime T0 = Seeds.Now;

    [Fact]
    public async Task The_owner_s_own_connections_receive_the_session_updates_with_the_power_drawn()
    {
        var hub = new RecordingHubContext<ChargingSessionHub>();
        await using var host = new OcppTestHost(relational: true, configure: s => s.AddSingleton<IHubContext<ChargingSessionHub>>(hub));
        var userId = 0;
        await host.Seed(async db =>
        {
            await Seeds.ChargePoint(db, Cp, c => { c.PricePerKWh = 2m; c.PricePerMinute = 0m; c.FlatFee = 0m; });
            var user = await Seeds.User(db);
            userId = user.ID;
            db.Cards.Add(Seeds.Card("TAG-LIVE", 200m, user.ID));
        });
        host.Sender.Versions[Cp] = OcppTestHost.Ocpp201;
        var charger = new Charger201(host, Cp);
        await charger.Boot();

        await charger.Started("TX-LIVE", "TAG-LIVE", 0, T0);
        await charger.Updated("TX-LIVE", 5000, T0.AddMinutes(10));
        await charger.Ended("TX-LIVE", 7000, T0.AddMinutes(20));

        var sessionId = await host.Query(db => db.ChargingSessions.Select(s => s.ID).SingleAsync());
        var mine = hub.ToGroup(ChargingSessionHub.GetUserGroupName(userId), "SessionUpdate").Select(a => (ChargingSessionUpdateDto)a[0]!).ToList();
        Assert.Equal(new[] { "Started", "Charging", "Ended" }, mine.Select(u => u.Status));
        Assert.All(mine, u => Assert.Equal(sessionId, u.SessionId));
        var charging = mine[1];
        Assert.Equal(5, charging.EnergyKWh, 3);
        Assert.Equal(30, charging.PowerKw); // 5 kWh in 10 minutes
        Assert.Equal(10m, charging.CurrentCost);
        // Followers of the session itself (operators) still get them; nobody else's user group does.
        Assert.Equal(3, hub.ToGroup(ChargingSessionHub.GetSessionGroupName(sessionId), "SessionUpdate").Count());
        Assert.Empty(hub.ToGroup(ChargingSessionHub.GetUserGroupName(userId + 1), "SessionUpdate"));
    }

    [Theory]
    [InlineData(0, 0, 5, 10, 30.0)]        // 5 kWh in 10 minutes
    [InlineData(5, 10, 5.5, 11, 30.0)]     // 0.5 kWh in one minute
    [InlineData(5, 10, 5, 20, 0.0)]        // idle: no energy
    [InlineData(5, 10, 5.1, 10.01, null)]  // readings less than 3 seconds apart
    [InlineData(5, 10, 4, 20, null)]       // meter went back
    public void The_power_is_the_energy_between_two_readings_over_their_interval(double fromKWh, double fromMinutes, double toKWh, double toMinutes, double? expected) =>
        Assert.Equal(expected, TransactionService.AveragePowerKw(fromKWh, fromMinutes, toKWh, toMinutes));

    [Fact]
    public async Task Each_connection_follows_its_own_user_s_sessions_from_the_token()
    {
        using var api = new ApiFactory();
        User user = null!;
        await api.Seed(async db => user = await Seeds.User(db, "live@test.ma"));
        await using var scope = api.Services.CreateAsyncScope();
        var groups = Recorder<IGroupManager>.Create();
        var hub = new ChargingSessionHub(NullLogger<ChargingSessionHub>.Instance, scope.ServiceProvider.GetRequiredService<AccessService>())
        {
            Context = new FakeHubCallerContext(FakeHubCallerContext.SignedIn(user.ID), "conn-7"),
            Groups = groups.Proxy
        };

        await hub.OnConnectedAsync();

        var added = Assert.Single(groups.Recorder.CallsTo(nameof(IGroupManager.AddToGroupAsync)));
        Assert.Equal("conn-7", added[0]);
        Assert.Equal(ChargingSessionHub.GetUserGroupName(user.ID), added[1]);
    }

    [Fact]
    public async Task A_connection_of_an_unknown_account_joins_no_group()
    {
        using var api = new ApiFactory();
        await using var scope = api.Services.CreateAsyncScope();
        var groups = Recorder<IGroupManager>.Create();
        var hub = new ChargingSessionHub(NullLogger<ChargingSessionHub>.Instance, scope.ServiceProvider.GetRequiredService<AccessService>())
        {
            Context = new FakeHubCallerContext(FakeHubCallerContext.SignedIn(424242)),
            Groups = groups.Proxy
        };

        await hub.OnConnectedAsync();

        Assert.Empty(groups.Recorder.CallsTo(nameof(IGroupManager.AddToGroupAsync)));
    }

    // ------------------------------------------------------------------ stopping one's own session

    private sealed class StopFixture : IAsyncDisposable
    {
        public FakeCommandSender Sender { get; } = new();
        public ApiFactory Api { get; }
        public User Owner = null!, Other = null!;
        public int SessionId;

        public StopFixture()
        {
            Api = new ApiFactory(services: s =>
            {
                s.RemoveAll<IOcppCommandSender>();
                s.AddSingleton<IOcppCommandSender>(Sender);
            });
        }

        public async Task Seed()
        {
            Sender.Versions[Cp] = OcppTestHost.Ocpp201;
            await Api.Seed(async db =>
            {
                Owner = await Seeds.User(db, "owner@test.ma");
                Other = await Seeds.User(db, "other@test.ma");
                var cp = await Seeds.ChargePoint(db, Cp, c => { c.PricePerKWh = 2m; });
                var connector = await db.Connectors.SingleAsync(c => c.ChargePointID == cp.ID);
                var card = Seeds.Card("OWN-1", 80m, Owner.ID);
                db.Cards.AddRange(card, Seeds.Card("OTH-1", 80m, Other.ID));
                await db.SaveChangesAsync();
                var session = new ChargingSession
                {
                    ConnectorID = connector.ID, UserID = Owner.ID, CardID = card.ID, StartDate = T0, ChargedKwhs = 3, ChargedMinutes = 6,
                    PricePerKWh = 2m, PricePerMinute = 0m, FlatFee = 0m, VatRate = 0.2m, ChargingSessionStatus = ChargingSessionStatusEnum.Pending
                };
                db.ChargingSessions.Add(session);
                await db.SaveChangesAsync();
                db.Transactions.Add(new Transaction
                {
                    Uid = "TX-MINE", ConnectorID = connector.ID, StartCardID = card.ID, ChargingSessionID = session.ID, StartTime = T0,
                    MeterStart = 0, Status = TransactionStatusEnum.Current
                });
                SessionId = session.ID;
            });
        }

        public ValueTask DisposeAsync()
        {
            Api.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task The_active_session_tells_its_owner_how_to_stop_it()
    {
        await using var f = new StopFixture();
        await f.Seed();

        var active = await f.Api.Client(f.Owner).GetFromJsonAsync<JsonElement>("/api/my/sessions/active");

        Assert.Equal(f.SessionId, active.GetProperty("id").GetInt32());
        Assert.Equal(Cp, active.GetProperty("chargePointName").GetString());
        Assert.Equal("TX-MINE", active.GetProperty("transactionUid").GetString());
        Assert.Equal(80m, active.GetProperty("cardBalance").GetDecimal());
        Assert.Equal(6m, active.GetProperty("currentCost").GetDecimal()); // 3 kWh x 2 MAD, VAT included
    }

    [Fact]
    public async Task The_owner_stops_their_session_and_the_command_is_audited()
    {
        await using var f = new StopFixture();
        await f.Seed();

        var response = await f.Api.Client(f.Owner).PostAsJsonAsync($"/ocpp/EVDriver/RequestStopTransactionMobile/{Cp}", new { transactionId = "TX-MINE" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Accepted", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        var sent = Assert.Single(f.Sender.Sent);
        Assert.Equal("RequestStopTransaction", sent.Action);
        Assert.Contains("TX-MINE", sent.Payload);
        Assert.True(await f.Api.Db(db => db.AuditLogs.AnyAsync(l => l.Action == "Ocpp.EVDriver.RequestStopTransactionMobile" && l.UserID == f.Owner.ID)));
    }

    [Fact]
    public async Task Nobody_else_can_stop_the_session()
    {
        await using var f = new StopFixture();
        await f.Seed();

        var other = await f.Api.Client(f.Other).PostAsJsonAsync($"/ocpp/EVDriver/RequestStopTransactionMobile/{Cp}", new { transactionId = "TX-MINE" });
        var anonymous = await f.Api.Client().PostAsJsonAsync($"/ocpp/EVDriver/RequestStopTransactionMobile/{Cp}", new { transactionId = "TX-MINE" });

        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Empty(f.Sender.Sent);
        Assert.Equal(HttpStatusCode.NoContent, (await f.Api.Client(f.Other).GetAsync("/api/my/sessions/active")).StatusCode);
    }
}
