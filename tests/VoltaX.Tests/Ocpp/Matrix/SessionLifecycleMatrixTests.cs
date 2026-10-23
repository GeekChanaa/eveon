using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using static VoltaX.Tests.Ocpp.Matrix.MatrixStation;

namespace VoltaX.Tests.Ocpp.Matrix;

/// <summary>How sessions start and end, for both protocols: cards, remote commands, unplugging, suspensions, reservations.</summary>
public class SessionLifecycleMatrixTests
{
    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Card_start_meter_values_and_stop_by_card_are_billed_once(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;

        await charger.Plug(T0);
        Assert.Equal(ConnectorStatusEnumType.Occupied, await ScenarioAssert.ConnectorStatus(host, Cp));
        Assert.Equal("Accepted", await charger.Authorize("TAG-1"));
        var (status, uid) = await charger.Start("TAG-1", 1000, T0);
        Assert.Equal("Accepted", status);
        await charger.Meter(uid, 6000, T0.AddMinutes(10));
        Assert.Equal("Accepted", await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1"));
        await charger.Unplug(T0.AddMinutes(32));

        var session = await ScenarioAssert.Billed(host, cardId, 100m, energy: 30.00m, time: 3.00m, flatFee: 1.00m);
        Assert.Equal(ReasonEnumType.Local, session.StoppedReason);
        Assert.Equal(ChargingSessionStatusEnum.Completed, session.ChargingSessionStatus);
        Assert.Equal(12, session.ChargedKwhs!.Value, 6);
        Assert.Equal(30, session.ChargedMinutes!.Value, 6);
        Assert.Equal(T0, session.StartDate);
        var tx = await Tx(host);
        Assert.Equal(TransactionStatusEnum.Ended, tx.Status);
        Assert.Equal(cardId, tx.StopCardID);
        Assert.Equal(T0.AddMinutes(30), tx.StopTime);
        Assert.Equal(ConnectorStatusEnumType.Available, await ScenarioAssert.ConnectorStatus(host, Cp));
        Assert.DoesNotContain("ChargingEndedAbnormally", await ScenarioAssert.Notifications(host));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Remote_start_and_remote_stop_reach_the_charger_in_its_protocol(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);

        var started = await host.With<IEVDriverService, RequestStartTransactionResponse>(s => s.RequestStartTransaction(Cp,
            new RequestStartTransactionRequest { IdToken = new IdTokenType { IdToken = "TAG-1", Type = IdTokenEnumType.Central }, EvseId = 1, RemoteStartId = 42 }));

        Assert.Equal(RequestStartStopStatusEnum.Accepted, started.Status);
        var start = Assert.Single(host.Sender.Sent);
        if (protocol == OcppTestHost.Ocpp16)
        {
            Assert.Equal("RemoteStartTransaction", start.Action);
            Assert.Contains("\"idTag\":\"TAG-1\"", start.Payload);
            Assert.Contains("\"connectorId\":1", start.Payload);
        }
        else
        {
            Assert.Equal("RequestStartTransaction", start.Action);
            Assert.Contains("\"remoteStartId\":42", start.Payload);
            ((ScenarioCharger201)charger).RemoteStartId = 42;
        }

        var (status, uid) = await charger.Start("TAG-1", 1000, T0, remote: true);
        Assert.Equal("Accepted", status);
        await charger.Meter(uid, 7000, T0.AddMinutes(15));

        var stopped = await host.With<IEVDriverService, RequestStopTransactionResponse>(s =>
            s.RequestStopTransaction(Cp, new RequestStopTransactionRequest { TransactionId = uid }));

        Assert.Equal(RequestStartStopStatusEnum.Accepted, stopped.Status);
        var stop = host.Sender.Sent.Last();
        if (protocol == OcppTestHost.Ocpp16)
        {
            Assert.Equal("RemoteStopTransaction", stop.Action);
            Assert.Contains($"\"transactionId\":{ScenarioCharger16.TransactionId(uid)}", stop.Payload);
        }
        else
        {
            Assert.Equal("RequestStopTransaction", stop.Action);
            Assert.Contains(uid, stop.Payload);
        }

        // No token in the stop message: the answer carries no token information either.
        Assert.Null(await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.Remote));
        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(ReasonEnumType.Remote, session.StoppedReason);
    }

    public static TheoryData<string, string, string> RefusedTokens => new()
    {
        { OcppTestHost.Ocpp16, "BLOCKED", "Blocked" },
        { OcppTestHost.Ocpp201, "BLOCKED", "Blocked" },
        { OcppTestHost.Ocpp16, "EXPIRED", "Expired" },
        { OcppTestHost.Ocpp201, "EXPIRED", "Expired" },
        { OcppTestHost.Ocpp16, "UNKNOWN-TAG", "Invalid" },
        { OcppTestHost.Ocpp201, "UNKNOWN-TAG", "Invalid" },
        { OcppTestHost.Ocpp16, "EMPTY", "Invalid" }, // 1.6 has no NoCredit
        { OcppTestHost.Ocpp201, "EMPTY", "NoCredit" },
    };

    [Theory, MemberData(nameof(RefusedTokens))]
    public async Task A_refused_token_opens_no_session_and_its_stop_is_acknowledged(string protocol, string token, string expected)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        var balances = await host.Query(db => db.Cards.ToDictionaryAsync(c => c.CardNumber!, c => c.Balance));

        Assert.Equal(expected, await charger.Authorize(token));
        await charger.Plug(T0);
        var (status, uid) = await charger.Start(token, 1000, T0);
        Assert.Equal(expected, status);
        // A 1.6 charger got a transactionId anyway and stops it; a 2.0.1 charger ends the deauthorized transaction.
        await charger.Stop(uid, 1000, T0.AddMinutes(1), StopKind.DeAuthorized);

        await ScenarioAssert.NothingBilled(host, cardId, 100m);
        Assert.Equal(balances, await host.Query(db => db.Cards.ToDictionaryAsync(c => c.CardNumber!, c => c.Balance)));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Unplugging_the_car_ends_the_session_normally(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(uid, 6000, T0.AddMinutes(10));

        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.EVDisconnected);
        await charger.Unplug(T0.AddMinutes(30));

        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(ReasonEnumType.EVDisconnected, session.StoppedReason);
        Assert.Equal(ChargingSessionStatusEnum.Completed, session.ChargingSessionStatus);
        Assert.Equal(ConnectorStatusEnumType.Available, await ScenarioAssert.ConnectorStatus(host, Cp));
        Assert.DoesNotContain("ChargingEndedAbnormally", await ScenarioAssert.Notifications(host));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task Suspended_EVSE_and_EV_keep_the_session_open_and_billed_on_time(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);
        await charger.Meter(uid, 6000, T0.AddMinutes(10));

        await charger.Suspend(uid, "SuspendedEVSE", 6000, T0.AddMinutes(15));   // e.g. the site power limit
        await charger.Resume(uid, 6000, T0.AddMinutes(25));
        await charger.Suspend(uid, "SuspendedEV", 13000, T0.AddMinutes(40));    // the car is full
        await charger.Meter(uid, 13000, T0.AddMinutes(50));

        Assert.Equal(TransactionStatusEnum.Current, (await Tx(host)).Status);
        Assert.Equal(ConnectorStatusEnumType.Occupied, await ScenarioAssert.ConnectorStatus(host, Cp));
        Assert.Equal(50, (await Session(host)).ChargedMinutes!.Value, 6);

        await charger.Stop(uid, 13000, T0.AddMinutes(60), StopKind.LocalCard, idTag: "TAG-1");
        await ScenarioAssert.Billed(host, cardId, 100m, energy: 30.00m, time: 6.00m, flatFee: 1.00m);
    }

    public static TheoryData<string, StopKind> StopReasons()
    {
        var data = new TheoryData<string, StopKind>();
        foreach (var protocol in new[] { OcppTestHost.Ocpp16, OcppTestHost.Ocpp201 })
            foreach (var kind in Enum.GetValues<StopKind>())
                data.Add(protocol, kind);
        return data;
    }

    [Theory, MemberData(nameof(StopReasons))]
    public async Task Every_stop_reason_is_recorded_and_abnormal_ones_are_reported_to_the_driver(string protocol, StopKind kind)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;
        await charger.Plug(T0);
        var (_, uid) = await charger.Start("TAG-1", 1000, T0);

        await charger.Stop(uid, 13000, T0.AddMinutes(30), kind, idTag: kind == StopKind.LocalCard ? "TAG-1" : null);

        var session = await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
        Assert.Equal(charger.StoredReason(kind), session.StoppedReason);
        Assert.Equal(ScenarioCharger.IsAbnormal(kind), (await ScenarioAssert.Notifications(host)).Contains("ChargingEndedAbnormally"));
    }

    [Theory, MemberData(nameof(Protocols), MemberType = typeof(MatrixStation))]
    public async Task A_reservation_is_used_by_the_transaction_started_with_it(string protocol)
    {
        var (host, charger, cardId) = await Open(protocol);
        await using var _ = host;

        var reserved = await host.With<IEVDriverService, ReserveNowResponse>(s => s.ReserveNow(Cp, new ReserveNowRequest
        {
            Id = 7,
            ExpiryDateTime = DateTime.UtcNow.AddHours(1),
            IdToken = new IdTokenType { IdToken = "TAG-1", Type = IdTokenEnumType.ISO14443 },
            EvseId = 1
        }));
        var cancelled = await host.With<IEVDriverService, ReserveNowResponse>(s => s.ReserveNow(Cp, new ReserveNowRequest
        {
            Id = 8,
            ExpiryDateTime = DateTime.UtcNow.AddHours(1),
            IdToken = new IdTokenType { IdToken = "TAG-2", Type = IdTokenEnumType.ISO14443 },
            EvseId = 1
        }));
        await host.With<IEVDriverService, CancelReservationResponse>(s => s.CancelReservation(Cp, new CancelReservationRequest { ReservationId = 8 }));

        Assert.Equal(ReserveNowStatusEnumType.Accepted, reserved.Status);
        Assert.Equal(ReserveNowStatusEnumType.Accepted, cancelled.Status);
        Assert.Equal(new[] { "ReserveNow", "ReserveNow", "CancelReservation" }, host.Sender.Sent.Select(s => s.Action));
        await charger.Plug(T0);
        var (status, uid) = await charger.Start("TAG-1", 1000, T0, reservationId: 7);
        await charger.Stop(uid, 13000, T0.AddMinutes(30), StopKind.LocalCard, idTag: "TAG-1");

        Assert.Equal("Accepted", status);
        var reservations = await host.Query(db => db.Reservations.OrderBy(r => r.ReservationId).ToListAsync());
        Assert.Equal(ReservationStatusEnum.Used, reservations[0].Status);
        Assert.Equal(uid, reservations[0].TransactionUid);
        Assert.Equal(ReservationStatusEnum.Cancelled, reservations[1].Status);
        await ScenarioAssert.Billed(host, cardId, 100m, 30.00m, 3.00m, 1.00m);
    }
}
