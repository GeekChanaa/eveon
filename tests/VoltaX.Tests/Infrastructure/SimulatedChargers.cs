using System.Text.Json;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaX.Tests.Infrastructure;

/// <summary>Seed data shared by the charging scenarios.</summary>
public static class Seeds
{
    public static readonly DateTime Now = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

    public static async Task<User> User(VoltaXApiDbContext db, string email = "driver@example.com")
    {
        var user = new User { Email = email, FirstName = "Ev", LastName = "Driver" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>A provisioned (bootable as Accepted) charge point, optionally with EVSE 1 / connector 1 priced.</summary>
    public static async Task<ChargePoint> ChargePoint(VoltaXApiDbContext db, string chargePointId, Action<Connector>? connector = null,
        int chargingStationId = 0)
    {
        var chargePoint = new ChargePoint { ChargePointId = chargePointId, SerialNumber = "SN-" + chargePointId, ChargingStationID = chargingStationId };
        db.ChargePoints.Add(chargePoint);
        await db.SaveChangesAsync();
        db.ChargePointProvisionings.Add(new ChargePointProvisioning { ChargePointID = chargePoint.ID, Status = ChargePointProvisioningStatusEnum.Provisioned });
        if (connector != null)
        {
            var row = new Connector { ChargePointID = chargePoint.ID, EvseID = 1, ConnectorID = 1 };
            connector(row);
            db.Connectors.Add(row);
        }
        await db.SaveChangesAsync();
        return chargePoint;
    }

    public static Card Card(string number, decimal balance, int? userId = null, bool blocked = false, DateTime? expires = null) => new()
    {
        CardNumber = number,
        Balance = balance,
        Blocked = blocked,
        ExpirationDate = expires ?? Now.AddYears(5),
        Status = CardStatusEnum.Active,
        UserID = userId,
        Note = ""
    };
}

/// <summary>An OCPP 1.6J charger driven through <see cref="OcppTestHost"/>.</summary>
public sealed class Charger16(OcppTestHost host, string id)
{
    public string Id => id;

    public Task<JsonElement> Call(string action, object payload) => host.Call(id, action, payload, OcppTestHost.Ocpp16);

    public Task<JsonElement> Boot() => Call("BootNotification", new { chargePointVendor = "Acme", chargePointModel = "AC22", chargePointSerialNumber = "SN-" + id });

    public Task<JsonElement> Status(int connectorId, string status, string errorCode = "NoError", DateTime? at = null) =>
        Call("StatusNotification", new { connectorId, errorCode, status, timestamp = at ?? Seeds.Now });

    public async Task<string> Authorize(string idTag) =>
        (await Call("Authorize", new { idTag })).GetProperty("idTagInfo").GetProperty("status").GetString()!;

    /// <summary>StartTransaction; returns (authorization status, transactionId).</summary>
    public async Task<(string Status, int TransactionId)> Start(string idTag, int meterStartWh, DateTime at, int connectorId = 1, int? reservationId = null)
    {
        var reply = await Call("StartTransaction", new { connectorId, idTag, meterStart = meterStartWh, timestamp = at, reservationId });
        return (reply.GetProperty("idTagInfo").GetProperty("status").GetString()!, reply.GetProperty("transactionId").GetInt32());
    }

    public Task<JsonElement> Meter(int transactionId, double energyWh, DateTime at, double? powerW = null, int connectorId = 1)
    {
        var values = new List<object> { new { value = energyWh.ToString(System.Globalization.CultureInfo.InvariantCulture), measurand = "Energy.Active.Import.Register", unit = "Wh", context = "Sample.Periodic" } };
        if (powerW != null)
            values.Add(new { value = powerW.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), measurand = "Power.Active.Import", unit = "W" });
        return Call("MeterValues", new { connectorId, transactionId, meterValue = new[] { new { timestamp = at, sampledValue = values } } });
    }

    public Task<JsonElement> Stop(int transactionId, int meterStopWh, DateTime at, string reason = "Local", string? idTag = null) =>
        Call("StopTransaction", new { transactionId, idTag, meterStop = meterStopWh, timestamp = at, reason });
}

/// <summary>An OCPP 2.0.1 charger driven through <see cref="OcppTestHost"/>.</summary>
public sealed class Charger201(OcppTestHost host, string id)
{
    private int _seqNo;

    public string Id => id;

    public Task<JsonElement> Call(string action, object payload) => host.Call(id, action, payload, OcppTestHost.Ocpp201);

    public Task<string> CallRaw(string action, object payload) => host.CallRaw(id, action, payload, OcppTestHost.Ocpp201);

    public Task<JsonElement> Boot(string reason = "PowerUp") => Call("BootNotification", new
    {
        reason,
        chargingStation = new { model = "DC50", vendorName = "Acme", serialNumber = "SN-" + id }
    });

    public Task<JsonElement> Status(string status, DateTime? at = null, int evseId = 1, int connectorId = 1) =>
        Call("StatusNotification", new { timestamp = (at ?? Seeds.Now).ToString("o"), connectorStatus = status, evseId, connectorId });

    public async Task<string> Authorize(string idToken) =>
        (await Call("Authorize", new { idToken = new { idToken, type = "ISO14443" } })).GetProperty("idTokenInfo").GetProperty("status").GetString()!;

    public static object Energy(double wh, DateTime at, string context = "Sample.Periodic") => new
    {
        timestamp = at.ToString("o"),
        sampledValue = new[] { new { value = wh, context, measurand = "Energy.Active.Import.Register", unitOfMeasure = new { unit = "Wh" } } }
    };

    public object Event(string eventType, string transactionId, string triggerReason, DateTime at, double? energyWh = null, string? idToken = null,
        bool withEvse = true, string? chargingState = null, string? stoppedReason = null, bool offline = false, int? seqNo = null, int? remoteStartId = null) => new
    {
        eventType,
        timestamp = at.ToString("o"),
        triggerReason,
        seqNo = seqNo ?? _seqNo++,
        offline,
        transactionInfo = new { transactionId, chargingState, stoppedReason, remoteStartId },
        evse = withEvse ? new { id = 1, connectorId = 1 } : null,
        idToken = idToken == null ? null : new { idToken, type = "ISO14443" },
        meterValue = energyWh == null ? null : new[] { Energy(energyWh.Value, at, eventType switch { "Started" => "Transaction.Begin", "Ended" => "Transaction.End", _ => "Sample.Periodic" }) }
    };

    public async Task<string?> Started(string transactionId, string idToken, double energyWh, DateTime at, string triggerReason = "Authorized", bool offline = false) =>
        IdTokenStatus(await Call("TransactionEvent", Event("Started", transactionId, triggerReason, at, energyWh, idToken, chargingState: "Charging", offline: offline)));

    public async Task<string?> Updated(string transactionId, double? energyWh, DateTime at, string triggerReason = "MeterValuePeriodic", string? chargingState = "Charging", bool offline = false) =>
        IdTokenStatus(await Call("TransactionEvent", Event("Updated", transactionId, triggerReason, at, energyWh, chargingState: chargingState, offline: offline)));

    public async Task<string?> Ended(string transactionId, double? energyWh, DateTime at, string triggerReason = "StopAuthorized", string stoppedReason = "Local",
        string? idToken = null, bool offline = false, int? seqNo = null) =>
        IdTokenStatus(await Call("TransactionEvent", Event("Ended", transactionId, triggerReason, at, energyWh, idToken, stoppedReason: stoppedReason, offline: offline, seqNo: seqNo)));

    private static string? IdTokenStatus(JsonElement reply) =>
        reply.TryGetProperty("idTokenInfo", out var info) ? info.GetProperty("status").GetString() : null;
}
