using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;

namespace VoltaX.Tests.PartnerPortal;

/// <summary>
/// Remote actions of the partner portal: a partner resets, unlocks and stops sessions on their own chargers only,
/// on 1.6 and 2.0.1 chargers alike, and every attempt is audited.
/// </summary>
public class PartnerRemoteCommandTests : IAsyncLifetime
{
    private readonly FakeCommandSender _sender = new();
    private readonly ApiFactory _api;
    private User _partnerA = null!, _partnerB = null!, _customer = null!;
    private int _cp201, _cp16, _cpB, _connector201, _connector16, _connectorB, _runningSession, _endedSession, _foreignSession;

    public PartnerRemoteCommandTests()
    {
        _api = new ApiFactory(services: s =>
        {
            s.RemoveAll<IOcppCommandSender>();
            s.AddSingleton<IOcppCommandSender>(_sender);
        });
    }

    public async Task InitializeAsync()
    {
        _sender.Versions["CP-A201"] = OcppTestHost.Ocpp201;
        _sender.Versions["CP-A16"] = OcppTestHost.Ocpp16;
        _sender.Versions["CP-B"] = OcppTestHost.Ocpp201;
        await _api.Seed(async db =>
        {
            var a = new Partner { Name = "Atlas Energy", Description = "", Email = "a@atlas.ma", Phone = "0600000001", PartnerIdentificationNumber = "P-A" };
            var b = new Partner { Name = "Borne Plus", Description = "", Email = "b@borne.ma", Phone = "0600000002", PartnerIdentificationNumber = "P-B" };
            db.Partners.AddRange(a, b);
            await db.SaveChangesAsync();
            _partnerA = await Seeds.User(db, "partner.a@test.ma", role: "Partner", partnerId: a.ID);
            _partnerB = await Seeds.User(db, "partner.b@test.ma", role: "Partner", partnerId: b.ID);
            _customer = await Seeds.User(db, "driver@test.ma");
            var stationA = await Seeds.Station(db, a.ID, "Atlas Agdal");
            var stationB = await Seeds.Station(db, b.ID, "Borne Maarif");
            _cp201 = (await Seeds.ChargePoint(db, "CP-A201", c => c.PricePerKWh = 3m, stationA.ID)).ID;
            _cp16 = (await Seeds.ChargePoint(db, "CP-A16", c => c.PricePerKWh = 3m, stationA.ID)).ID;
            _cpB = (await Seeds.ChargePoint(db, "CP-B", c => c.PricePerKWh = 3m, stationB.ID)).ID;
            _connector201 = (await db.Connectors.SingleAsync(c => c.ChargePointID == _cp201)).ID;
            _connector16 = (await db.Connectors.SingleAsync(c => c.ChargePointID == _cp16)).ID;
            _connectorB = (await db.Connectors.SingleAsync(c => c.ChargePointID == _cpB)).ID;

            var card = Seeds.Card("DRV-1", 300m, _customer.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            ChargingSession Session(int connectorId, DateTime? end) => new()
            {
                ConnectorID = connectorId, CardID = card.ID, UserID = _customer.ID, StartDate = Seeds.Now, EndDate = end, ChargedKwhs = 2, ChargedMinutes = 5,
                ChargingSessionStatus = end == null ? ChargingSessionStatusEnum.Pending : ChargingSessionStatusEnum.Completed
            };
            var running = Session(_connector201, null);
            var ended = Session(_connector16, Seeds.Now.AddMinutes(30));
            var foreign = Session(_connectorB, null);
            db.ChargingSessions.AddRange(running, ended, foreign);
            await db.SaveChangesAsync();
            db.Transactions.AddRange(
                new Transaction { Uid = "TX-A", ConnectorID = _connector201, StartCardID = card.ID, ChargingSessionID = running.ID, StartTime = Seeds.Now, Status = TransactionStatusEnum.Current },
                new Transaction { Uid = "TX-B", ConnectorID = _connectorB, StartCardID = card.ID, ChargingSessionID = foreign.ID, StartTime = Seeds.Now, Status = TransactionStatusEnum.Current });
            _runningSession = running.ID;
            _endedSession = ended.ID;
            _foreignSession = foreign.ID;
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<string?> Status(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString();

    private Task<bool> Audited(string action, string chargePointId) =>
        _api.Db(db => db.AuditLogs.AnyAsync(l => l.Action == action && l.EntityID == chargePointId && l.UserID == _partnerA.ID));

    [Fact]
    public async Task A_partner_resets_their_own_chargers_soft_or_hard_on_both_protocols()
    {
        var client = _api.Client(_partnerA);

        var soft = await client.PostAsJsonAsync($"/api/partner-portal/charge-points/{_cp201}/reset", new { type = "Soft" });
        var hard = await client.PostAsJsonAsync($"/api/partner-portal/charge-points/{_cp16}/reset", new { type = "Hard" });

        Assert.Equal(HttpStatusCode.OK, soft.StatusCode);
        Assert.Equal("Accepted", await Status(soft));
        Assert.Equal(HttpStatusCode.OK, hard.StatusCode);
        Assert.Equal(("CP-A201", "Reset"), (_sender.Sent[0].ChargePointId, _sender.Sent[0].Action));
        Assert.Contains("\"OnIdle\"", _sender.Sent[0].Payload);
        Assert.Equal(("CP-A16", "Reset"), (_sender.Sent[1].ChargePointId, _sender.Sent[1].Action));
        Assert.Contains("\"Hard\"", _sender.Sent[1].Payload); // OCPP 1.6 wording
        Assert.True(await Audited("PartnerPortal.Reset", "CP-A201"));
        Assert.True(await Audited("PartnerPortal.Reset", "CP-A16"));
    }

    [Fact]
    public async Task A_reset_needs_its_type()
    {
        var response = await _api.Client(_partnerA).PostAsJsonAsync($"/api/partner-portal/charge-points/{_cp201}/reset", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_sender.Sent);
    }

    [Fact]
    public async Task A_partner_unlocks_their_own_connector()
    {
        _sender.Reply = action => action == "UnlockConnector" ? new { status = "Unlocked" } : new { status = "Accepted" };

        var response = await _api.Client(_partnerA).PostAsync($"/api/partner-portal/connectors/{_connector201}/unlock", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Unlocked", await Status(response));
        var sent = Assert.Single(_sender.Sent);
        Assert.Equal("UnlockConnector", sent.Action);
        Assert.Contains("\"evseId\":1", sent.Payload);
        Assert.True(await Audited("PartnerPortal.UnlockConnector", "CP-A201"));
    }

    [Fact]
    public async Task A_partner_stops_a_running_session_on_their_station()
    {
        var response = await _api.Client(_partnerA).PostAsync($"/api/partner-portal/sessions/{_runningSession}/stop", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sent = Assert.Single(_sender.Sent);
        Assert.Equal(("CP-A201", "RequestStopTransaction"), (sent.ChargePointId, sent.Action));
        Assert.Contains("TX-A", sent.Payload);
        Assert.True(await Audited("PartnerPortal.StopSession", "CP-A201"));
    }

    [Fact]
    public async Task Stopping_a_session_that_already_ended_sends_nothing()
    {
        var response = await _api.Client(_partnerA).PostAsync($"/api/partner-portal/sessions/{_endedSession}/stop", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NotRunning", await Status(response));
        Assert.Empty(_sender.Sent);
    }

    [Fact]
    public async Task A_partner_asks_their_charger_for_its_connectors_status()
    {
        var response = await _api.Client(_partnerA).PostAsync($"/api/partner-portal/charge-points/{_cp201}/refresh-status", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sent = Assert.Single(_sender.Sent);
        Assert.Equal("TriggerMessage", sent.Action);
        Assert.Contains("StatusNotification", sent.Payload);
        Assert.True(await Audited("PartnerPortal.TriggerStatus", "CP-A201"));
    }

    [Fact]
    public async Task Nobody_acts_on_another_partner_s_chargers()
    {
        var a = _api.Client(_partnerA);

        var calls = new[]
        {
            await a.PostAsJsonAsync($"/api/partner-portal/charge-points/{_cpB}/reset", new { type = "Hard" }),
            await a.PostAsync($"/api/partner-portal/charge-points/{_cpB}/refresh-status", null),
            await a.PostAsync($"/api/partner-portal/connectors/{_connectorB}/unlock", null),
            await a.PostAsync($"/api/partner-portal/sessions/{_foreignSession}/stop", null)
        };

        Assert.All(calls, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.Client(_customer).PostAsync($"/api/partner-portal/sessions/{_runningSession}/stop", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.Client().PostAsync($"/api/partner-portal/connectors/{_connector201}/unlock", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.Client(_partnerB).PostAsync($"/api/partner-portal/connectors/{_connector16}/unlock", null)).StatusCode);
        Assert.Empty(_sender.Sent);
    }

    [Fact]
    public async Task An_offline_charger_answers_409_and_the_attempt_is_still_audited()
    {
        _sender.Reply = _ => throw new WebSocketNotFoundException("CP-A201 is not connected");

        var response = await _api.Client(_partnerA).PostAsJsonAsync($"/api/partner-portal/charge-points/{_cp201}/reset", new { type = "Soft" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NotConnected", await Status(response));
        var log = await _api.Db(db => db.AuditLogs.SingleAsync(l => l.Action == "PartnerPortal.Reset"));
        Assert.Contains("WebSocketNotFoundException", log.ChangesJson);
    }

    [Fact]
    public async Task Charge_points_show_the_running_session_and_whether_the_charger_is_connected()
    {
        _sender.Versions.Remove("CP-A16");

        var points = await _api.Client(_partnerA).GetFromJsonAsync<JsonElement[]>("/api/partner-portal/charge-points");

        var live = points!.Single(p => p.GetProperty("chargePointId").GetString() == "CP-A201");
        var offline = points!.Single(p => p.GetProperty("chargePointId").GetString() == "CP-A16");
        Assert.True(live.GetProperty("online").GetBoolean());
        Assert.Equal("ocpp2.0.1", live.GetProperty("protocol").GetString());
        Assert.Equal(_runningSession, live.GetProperty("connectors")[0].GetProperty("activeSessionId").GetInt32());
        Assert.False(offline.GetProperty("online").GetBoolean());
        Assert.Equal(JsonValueKind.Null, offline.GetProperty("connectors")[0].GetProperty("activeSessionId").ValueKind);
    }
}
