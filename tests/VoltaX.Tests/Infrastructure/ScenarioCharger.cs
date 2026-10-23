using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services;

namespace VoltaX.Tests.Infrastructure;

/// <summary>Why a transaction ends, in protocol-neutral terms; each driver sends its version's wire values.</summary>
public enum StopKind
{
    /// <summary>The driver badges the card again (1.6 Local + idTag, 2.0.1 StopAuthorized/Local + idToken).</summary>
    LocalCard,
    /// <summary>RemoteStopTransaction / RequestStopTransaction (1.6 Remote, 2.0.1 RemoteStop/Remote).</summary>
    Remote,
    /// <summary>The car is unplugged (1.6 EVDisconnected, 2.0.1 EVCommunicationLost/EVDisconnected).</summary>
    EVDisconnected,
    PowerLoss,
    /// <summary>The token is no longer valid (1.6 DeAuthorized, 2.0.1 Deauthorized/DeAuthorized).</summary>
    DeAuthorized,
    EmergencyStop,
    Reboot,
    /// <summary>A hardware fault (1.6 Other, 2.0.1 AbnormalCondition/OvercurrentFault).</summary>
    Fault
}

/// <summary>
/// One charger of either protocol driven through <see cref="OcppTestHost"/> with the same vocabulary, so a scenario
/// runs unchanged against OCPP 1.6J and 2.0.1. Transactions are referred to by the CSMS uid (2.0.1 transaction id,
/// 1.6 <c>ocpp16-N</c>). The message order follows real chargers: the transaction message goes before the status it
/// leads to.
/// </summary>
public abstract class ScenarioCharger
{
    public const string ProtocolOcpp16 = OcppTestHost.Ocpp16;
    public const string ProtocolOcpp201 = OcppTestHost.Ocpp201;

    protected ScenarioCharger(OcppTestHost host, string id)
    {
        Host = host;
        Id = id;
    }

    public OcppTestHost Host { get; }
    public string Id { get; }
    public abstract string Protocol { get; }

    public static ScenarioCharger For(OcppTestHost host, string protocol, string id)
    {
        host.Sender.Versions[id] = protocol;
        return protocol == ProtocolOcpp16 ? new ScenarioCharger16(host, id) : new ScenarioCharger201(host, id);
    }

    /// <summary>BootNotification; returns the registration status.</summary>
    public abstract Task<string> Boot();

    /// <summary>Authorize; returns the idTagInfo / idTokenInfo status.</summary>
    public abstract Task<string> Authorize(string idTag);

    /// <summary>The car is plugged in (1.6 Preparing, 2.0.1 Occupied).</summary>
    public abstract Task Plug(DateTime at);

    /// <summary>Starts a transaction with a card (or a remote start); returns the authorization answer and the transaction uid.</summary>
    public abstract Task<(string Status, string Uid)> Start(string idTag, double meterWh, DateTime at, bool remote = false, int? reservationId = null,
        bool offline = false);

    public abstract Task Meter(string uid, double energyWh, DateTime at, bool offline = false);

    /// <summary>Charging pauses: SuspendedEV (the car) or SuspendedEVSE (the charger).</summary>
    public abstract Task Suspend(string uid, string state, double energyWh, DateTime at);

    /// <summary>Charging resumes after a suspension.</summary>
    public abstract Task Resume(string uid, double energyWh, DateTime at);

    /// <summary>Ends the transaction; returns the idTagInfo / idTokenInfo status of the answer, if any.</summary>
    public abstract Task<string?> Stop(string uid, double energyWh, DateTime at, StopKind kind, string? idTag = null, bool offline = false);

    /// <summary>The car leaves (Available).</summary>
    public abstract Task Unplug(DateTime at);

    public abstract Task Fault(DateTime at);

    /// <summary>The fault clears while the car is still plugged in.</summary>
    public abstract Task Recover(DateTime at);

    /// <summary>The connector is out of service (reboot, ChangeAvailability).</summary>
    public abstract Task Unavailable(DateTime at);

    /// <summary>The value stored on the session for a stop of this kind.</summary>
    public abstract ReasonEnumType StoredReason(StopKind kind);

    /// <summary>Is a stop of this kind reported to the driver as abnormal ("ChargingEndedAbnormally")?</summary>
    public static bool IsAbnormal(StopKind kind) => kind is StopKind.PowerLoss or StopKind.EmergencyStop or StopKind.Reboot or StopKind.Fault;

    protected static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);

    private (string Action, object Payload)? _lastTransactionMessage;

    /// <summary>Sends a transaction-related message and remembers it for <see cref="RepeatLast"/>.</summary>
    protected Task<JsonElement> SendTransactionMessage(string action, object payload)
    {
        _lastTransactionMessage = (action, payload);
        return Host.Call(Id, action, payload, Protocol);
    }

    /// <summary>Sends the last transaction message again, unchanged (same payload, same seqNo), as after a lost CALLRESULT.</summary>
    public Task<JsonElement> RepeatLast() =>
        Host.Call(Id, _lastTransactionMessage!.Value.Action, _lastTransactionMessage.Value.Payload, Protocol);

    /// <summary>The authorization status and transaction uid in the answer to a start (<paramref name="sentUid"/>: the uid the charger sent, 2.0.1).</summary>
    public abstract (string Status, string Uid) ReadStart(JsonElement reply, string sentUid);
}

public sealed class ScenarioCharger16(OcppTestHost host, string id) : ScenarioCharger(host, id)
{
    public override string Protocol => ProtocolOcpp16;

    private Task<JsonElement> Call(string action, object payload) => Host.Call(Id, action, payload, OcppTestHost.Ocpp16);

    private Task Status(string status, DateTime at, string errorCode = "NoError") =>
        Call("StatusNotification", new { connectorId = 1, errorCode, status, timestamp = at });

    public static int TransactionId(string uid) => int.Parse(uid["ocpp16-".Length..], CultureInfo.InvariantCulture);

    public override async Task<string> Boot() =>
        (await Call("BootNotification", new { chargePointVendor = "Acme", chargePointModel = "AC22", chargePointSerialNumber = "SN-" + Id }))
        .GetProperty("status").GetString()!;

    public override async Task<string> Authorize(string idTag) =>
        (await Call("Authorize", new { idTag })).GetProperty("idTagInfo").GetProperty("status").GetString()!;

    public override Task Plug(DateTime at) => Status("Preparing", at);

    public override async Task<(string Status, string Uid)> Start(string idTag, double meterWh, DateTime at, bool remote = false, int? reservationId = null,
        bool offline = false)
    {
        var reply = await SendTransactionMessage("StartTransaction", new { connectorId = 1, idTag, meterStart = (int)meterWh, timestamp = at, reservationId });
        var (status, uid) = ReadStart(reply, "");
        if (!offline)
            await Status(status == "Accepted" ? "Charging" : "Finishing", at);
        return (status, uid);
    }

    public override (string Status, string Uid) ReadStart(JsonElement reply, string sentUid) =>
        (reply.GetProperty("idTagInfo").GetProperty("status").GetString()!, $"ocpp16-{reply.GetProperty("transactionId").GetInt32()}");

    public override Task Meter(string uid, double energyWh, DateTime at, bool offline = false) =>
        SendTransactionMessage("MeterValues", new
        {
            connectorId = 1,
            transactionId = TransactionId(uid),
            meterValue = new[]
            {
                new
                {
                    timestamp = at,
                    sampledValue = new object[]
                    {
                        new { value = Number(energyWh), measurand = "Energy.Active.Import.Register", unit = "Wh", context = "Sample.Periodic" },
                        new { value = "7400", measurand = "Power.Active.Import", unit = "W", context = "Sample.Periodic" }
                    }
                }
            }
        });

    public override async Task Suspend(string uid, string state, double energyWh, DateTime at)
    {
        await Status(state, at);
        await Meter(uid, energyWh, at);
    }

    public override async Task Resume(string uid, double energyWh, DateTime at)
    {
        await Status("Charging", at);
        await Meter(uid, energyWh, at);
    }

    public override async Task<string?> Stop(string uid, double energyWh, DateTime at, StopKind kind, string? idTag = null, bool offline = false)
    {
        var reply = await SendTransactionMessage("StopTransaction", new
        {
            transactionId = TransactionId(uid),
            idTag,
            meterStop = (int)energyWh,
            timestamp = at,
            reason = Reason(kind)
        });
        if (!offline && kind != StopKind.EVDisconnected)
            await Status("Finishing", at);
        return reply.TryGetProperty("idTagInfo", out var info) ? info.GetProperty("status").GetString() : null;
    }

    public override Task Unplug(DateTime at) => Status("Available", at);

    public override Task Fault(DateTime at) => Status("Faulted", at, errorCode: "GroundFailure");

    public override Task Recover(DateTime at) => Status("Finishing", at);

    public override Task Unavailable(DateTime at) => Status("Unavailable", at);

    public static string Reason(StopKind kind) => kind switch
    {
        StopKind.LocalCard => "Local",
        StopKind.Remote => "Remote",
        StopKind.EVDisconnected => "EVDisconnected",
        StopKind.PowerLoss => "PowerLoss",
        StopKind.DeAuthorized => "DeAuthorized",
        StopKind.EmergencyStop => "EmergencyStop",
        StopKind.Reboot => "Reboot",
        _ => "Other"
    };

    public override ReasonEnumType StoredReason(StopKind kind) => kind switch
    {
        StopKind.LocalCard => ReasonEnumType.Local,
        StopKind.Remote => ReasonEnumType.Remote,
        StopKind.EVDisconnected => ReasonEnumType.EVDisconnected,
        StopKind.PowerLoss => ReasonEnumType.PowerLoss,
        StopKind.DeAuthorized => ReasonEnumType.DeAuthorized,
        StopKind.EmergencyStop => ReasonEnumType.EmergencyStop,
        StopKind.Reboot => ReasonEnumType.Reboot,
        _ => ReasonEnumType.Other
    };
}

public sealed class ScenarioCharger201(OcppTestHost host, string id) : ScenarioCharger(host, id)
{
    private readonly Dictionary<string, int> _seqNo = new();
    private int _transactions;

    public override string Protocol => ProtocolOcpp201;

    /// <summary>The remoteStartId the next remote start reports (the CSMS chose it in RequestStartTransaction).</summary>
    public int? RemoteStartId { get; set; }

    private Task<JsonElement> Call(string action, object payload) => Host.Call(Id, action, payload, OcppTestHost.Ocpp201);

    private Task Status(string status, DateTime at) =>
        Call("StatusNotification", new { timestamp = at.ToString("o"), connectorStatus = status, evseId = 1, connectorId = 1 });

    private int NextSeqNo(string uid)
    {
        _seqNo.TryGetValue(uid, out var next);
        _seqNo[uid] = next + 1;
        return next;
    }

    public static object EnergySample(double wh, DateTime at, string context) => new
    {
        timestamp = at.ToString("o"),
        sampledValue = new object[]
        {
            new { value = wh, context, measurand = "Energy.Active.Import.Register", unitOfMeasure = new { unit = "Wh" } },
            new { value = 7400, context, measurand = "Power.Active.Import", unitOfMeasure = new { unit = "W" } }
        }
    };

    /// <summary>A TransactionEvent with this charger's next seqNo for the transaction.</summary>
    public Task<JsonElement> Event(string eventType, string uid, string triggerReason, DateTime at, double? energyWh, string? idToken = null,
        string? chargingState = null, string? stoppedReason = null, bool offline = false, int? reservationId = null, int? remoteStartId = null,
        bool withEvse = true, int? seqNo = null) =>
        SendTransactionMessage("TransactionEvent", new
        {
            eventType,
            timestamp = at.ToString("o"),
            triggerReason,
            seqNo = seqNo ?? NextSeqNo(uid),
            offline,
            reservationId,
            transactionInfo = new { transactionId = uid, chargingState, stoppedReason, remoteStartId },
            evse = withEvse ? new { id = 1, connectorId = 1 } : null,
            idToken = idToken == null ? null : new { idToken, type = "ISO14443" },
            meterValue = energyWh == null
                ? null
                : new[] { EnergySample(energyWh.Value, at, eventType switch { "Started" => "Transaction.Begin", "Ended" => "Transaction.End", _ => "Sample.Periodic" }) }
        });

    public static string? TokenStatus(JsonElement reply) =>
        reply.TryGetProperty("idTokenInfo", out var info) && info.ValueKind == JsonValueKind.Object ? info.GetProperty("status").GetString() : null;

    public override async Task<string> Boot() =>
        (await Call("BootNotification", new { reason = "PowerUp", chargingStation = new { model = "DC50", vendorName = "Acme", serialNumber = "SN-" + Id } }))
        .GetProperty("status").GetString()!;

    public override async Task<string> Authorize(string idTag) =>
        TokenStatus(await Call("Authorize", new { idToken = new { idToken = idTag, type = "ISO14443" } }))!;

    public override Task Plug(DateTime at) => Status("Occupied", at);

    public override async Task<(string Status, string Uid)> Start(string idTag, double meterWh, DateTime at, bool remote = false, int? reservationId = null,
        bool offline = false)
    {
        var uid = $"{Id}-TX{++_transactions}";
        var reply = await Event("Started", uid, remote ? "RemoteStart" : "Authorized", at, meterWh, idTag, "Charging", offline: offline,
            reservationId: reservationId, remoteStartId: remote ? RemoteStartId : null);
        return ReadStart(reply, uid);
    }

    public override (string Status, string Uid) ReadStart(JsonElement reply, string sentUid) => (TokenStatus(reply)!, sentUid);

    public override Task Meter(string uid, double energyWh, DateTime at, bool offline = false) =>
        Event("Updated", uid, "MeterValuePeriodic", at, energyWh, chargingState: "Charging", offline: offline);

    public override Task Suspend(string uid, string state, double energyWh, DateTime at) =>
        Event("Updated", uid, "ChargingStateChanged", at, energyWh, chargingState: state);

    public override Task Resume(string uid, double energyWh, DateTime at) =>
        Event("Updated", uid, "ChargingStateChanged", at, energyWh, chargingState: "Charging");

    public override async Task<string?> Stop(string uid, double energyWh, DateTime at, StopKind kind, string? idTag = null, bool offline = false)
    {
        var (trigger, reason) = Wire(kind);
        return TokenStatus(await Event("Ended", uid, trigger, at, energyWh, idTag, kind == StopKind.EVDisconnected ? "Idle" : "EVConnected",
            reason, offline));
    }

    public override Task Unplug(DateTime at) => Status("Available", at);

    public override Task Fault(DateTime at) => Status("Faulted", at);

    public override Task Recover(DateTime at) => Status("Occupied", at);

    public override Task Unavailable(DateTime at) => Status("Unavailable", at);

    public static (string TriggerReason, string StoppedReason) Wire(StopKind kind) => kind switch
    {
        StopKind.LocalCard => ("StopAuthorized", "Local"),
        StopKind.Remote => ("RemoteStop", "Remote"),
        StopKind.EVDisconnected => ("EVCommunicationLost", "EVDisconnected"),
        StopKind.PowerLoss => ("AbnormalCondition", "PowerLoss"),
        StopKind.DeAuthorized => ("Deauthorized", "DeAuthorized"),
        StopKind.EmergencyStop => ("AbnormalCondition", "EmergencyStop"),
        StopKind.Reboot => ("AbnormalCondition", "Reboot"),
        _ => ("AbnormalCondition", "OvercurrentFault")
    };

    public override ReasonEnumType StoredReason(StopKind kind) => Enum.Parse<ReasonEnumType>(Wire(kind).StoppedReason);
}

/// <summary>Assertions shared by the charging scenarios: session, bill, VAT, wallet ledger and connector status.</summary>
public static class ScenarioAssert
{
    public const decimal VatRate = 0.2m;

    /// <summary>
    /// The only session is ended and billed with these components (VAT included), the VAT split adds up, the
    /// ledger holds exactly one session debit (and one idle-fee debit when there is an idle amount), and the card
    /// balance is the opening balance minus the total and equals the ledger sum.
    /// </summary>
    public static async Task<ChargingSession> Billed(OcppTestHost host, int cardId, decimal openingBalance, decimal energy, decimal time, decimal flatFee,
        decimal idle = 0m)
    {
        var session = await host.Query(db => db.ChargingSessions.SingleAsync());
        var charge = energy + time + flatFee;
        Assert.NotNull(session.EndDate);
        Assert.Equal(energy, session.EnergyAmount);
        Assert.Equal(time, session.TimeAmount);
        Assert.Equal(flatFee, session.FlatFeeAmount);
        Assert.Equal(charge + idle, session.TotalAmount);
        Assert.Equal(VatRate, session.VatRate);
        var (excl, vat) = CostCalculator.SplitVat(charge, VatRate);
        Assert.Equal(excl, session.ChargingPriceWithoutVAT(VatRate));
        Assert.Equal(vat, session.VATChargingPrice(VatRate));
        Assert.Equal(charge, session.ChargingPriceWithoutVAT(VatRate) + session.VATChargingPrice(VatRate));

        var movements = await host.Query(db => db.WalletTransactions.Where(w => w.CardID == cardId && w.ChargingSessionID != null).ToListAsync());
        var debits = movements.Where(m => m.Type == WalletTransactionType.SessionCharge).ToList();
        if (charge > 0)
            Assert.Equal(-charge, Assert.Single(debits).Amount);
        else
            Assert.Empty(debits);
        var idleDebits = movements.Where(m => m.Type == WalletTransactionType.IdleFee).ToList();
        if (idle > 0)
            Assert.Equal(-idle, Assert.Single(idleDebits).Amount);
        else
            Assert.Empty(idleDebits);

        var balance = await Balance(host, cardId);
        Assert.Equal(openingBalance - charge - idle, balance);
        Assert.Equal(balance, await host.Query(db => db.WalletTransactions.Where(w => w.CardID == cardId).SumAsync(w => w.Amount)));
        Assert.Equal(charge, await host.Query(db => db.Transactions.Where(t => t.ChargingSessionID == session.ID).Select(t => t.Amount).SingleAsync()));
        return session;
    }

    /// <summary>No session, no transaction, no money moved.</summary>
    public static async Task NothingBilled(OcppTestHost host, int cardId, decimal openingBalance)
    {
        Assert.False(await host.Query(db => db.ChargingSessions.AnyAsync()));
        Assert.False(await host.Query(db => db.Transactions.AnyAsync()));
        Assert.False(await host.Query(db => db.WalletTransactions.AnyAsync(w => w.CardID == cardId && w.ChargingSessionID != null)));
        Assert.Equal(openingBalance, await Balance(host, cardId));
    }

    public static Task<decimal> Balance(OcppTestHost host, int cardId) =>
        host.Query(db => db.Cards.Where(c => c.ID == cardId).Select(c => c.Balance).SingleAsync());

    public static Task<ConnectorStatusEnumType> ConnectorStatus(OcppTestHost host, string chargePointId) =>
        host.Query(db => db.ConnectorStatuses.Where(s => s.Connector!.ChargePoint!.ChargePointId == chargePointId)
            .Select(s => s.LastStatus).SingleAsync());

    public static Task<List<string>> Notifications(OcppTestHost host) =>
        host.Query(db => db.Notifications.Select(n => n.Action!).ToListAsync());
}
