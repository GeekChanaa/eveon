using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Ocpp16;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

// In-process OCPP 1.6 charger driving the real handlers and services against a private in-memory database.
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}

var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Ocpp:CommandTimeoutSeconds"] = "2",
    ["GlobalConfigurations:DefaultLowBalanceThresholdBuffer"] = "0",
    ["GlobalConfigurations:DefaultPricePerKwh"] = "2"
}).Build();

var sender = new FakeCommandSender();
var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole().SetMinimumLevel(LogLevel.Error));
services.AddSingleton<IConfiguration>(config);
services.AddSignalR();
services.AddMemoryCache();
services.AddHttpContextAccessor();
var databaseName = Guid.NewGuid().ToString();
services.AddDbContext<VoltaXApiDbContext>(o => o.UseInMemoryDatabase(databaseName)
    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
VoltaXApi.ServiceRegistration.ConfigureOptions(services, config);
VoltaXApi.ServiceRegistration.ConfigureRepositories(services);
VoltaXApi.ServiceRegistration.ConfigureFactories(services);
VoltaXApi.ServiceRegistration.ConfigureApplicationServices(services);
VoltaXApi.ServiceRegistration.ConfigureOCPPServices(services);
VoltaXApi.ServiceRegistration.ConfigureOCPPHandlers(services);
VoltaXApi.ServiceRegistration.ConfigureAutoMapper(services);
// Outside world replaced: no mail, no OCPI, no real sockets for CSMS commands.
services.AddScoped(_ => Stub<IMailService>.Create());
services.AddScoped(_ => Stub<ISystemReportService>.Create());
services.AddSingleton(_ => Stub<IWebHostEnvironment>.Create());
services.AddScoped<IExternalTokenAuthorizer, NoExternalTokens>();
services.AddScoped<IRoamingTransactionObserver, NoRoaming>();
services.AddSingleton<IOcppCommandSender>(sender);
// Load balancing has its own suite (VoltaX.SmartChargingChecks); here it must not send anything.
services.AddSingleton<VoltaXApi.SmartCharging.ILoadBalancingTrigger, NoLoadBalancing>();
var provider = services.BuildServiceProvider();

// ---------------------------------------------------------------- data
const string Cp16 = "CP16";
const string CpPending = "CP16-PENDING";
const string Cp201 = "CP201";
await using (var scope = provider.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
    var user = new User { Email = "driver@example.com", FirstName = "Ev", LastName = "Driver" };
    db.Users.Add(user);
    var accepted = new ChargePoint { ChargePointId = Cp16, SerialNumber = "S16" };
    var pending = new ChargePoint { ChargePointId = CpPending, SerialNumber = "S16P" };
    var cp201 = new ChargePoint { ChargePointId = Cp201, SerialNumber = "S201" };
    db.ChargePoints.AddRange(accepted, pending, cp201);
    await db.SaveChangesAsync();
    db.ChargePointProvisionings.AddRange(
        new ChargePointProvisioning { ChargePointID = accepted.ID, Status = ChargePointProvisioningStatusEnum.Provisioned },
        new ChargePointProvisioning { ChargePointID = cp201.ID, Status = ChargePointProvisioningStatusEnum.Provisioned });
    db.Connectors.Add(new Connector { ChargePointID = cp201.ID, EvseID = 1, ConnectorID = 1, PricePerMinute = 0.5 });
    db.Cards.AddRange(
        new Card { CardNumber = "TAG1", Balance = 100, ExpirationDate = DateTime.UtcNow.AddYears(1), Status = CardStatusEnum.Active, UserID = user.ID, Note = "" },
        new Card { CardNumber = "BLOCKED", Balance = 100, Blocked = true, ExpirationDate = DateTime.UtcNow.AddYears(1), Status = CardStatusEnum.Active, Note = "" },
        new Card { CardNumber = "EMPTY", Balance = 0, ExpirationDate = DateTime.UtcNow.AddYears(1), Status = CardStatusEnum.Active, Note = "" });
    await db.SaveChangesAsync();
}

// ---------------------------------------------------------------- negotiation
var registry = provider.GetRequiredService<OcppInboundHandlerRegistry>();
Check(registry.SupportedProtocols.Contains(OcppProtocols.Ocpp16) && registry.SupportedProtocols[0] == OcppProtocols.Ocpp201, "1.6 handlers registered; 2.0.1 still preferred");
var matcher = provider.GetRequiredService<WebSocketSubProtocolMatcher>();
Check(matcher.GetMatchingSubProtocol(new[] { "ocpp1.6" }) == "ocpp1.6", "a 1.6-only charger negotiates ocpp1.6");

// ---------------------------------------------------------------- simulated charger
var requestHandler = provider.GetRequiredService<OCPPRequestHandler>();
var connections = new Dictionary<string, OcppConnection>();
OcppConnection Connection(string id, string protocol = OcppProtocols.Ocpp16)
{
    if (!connections.TryGetValue(id, out var connection))
        connections[id] = connection = new OcppConnection(id, protocol, new IdleWebSocket(), new ChargePointStatus { Id = id, Protocol = protocol });
    return connection;
}
var messageCounter = 0;
async Task<JsonElement> Call(string chargePointId, string action, object payload, string protocol = OcppProtocols.Ocpp16)
{
    var id = $"m{++messageCounter}";
    var json = JsonConvert.SerializeObject(payload, OCPPMessageFactory.DefaultSettings);
    var raw = OcppJson.SerializeCall(id, action, json);
    var frame = OcppJson.Parse(raw).Frame!;
    var reply = await requestHandler.ProcessRequest(Connection(chargePointId, protocol), frame, raw);
    var elements = JsonDocument.Parse(reply).RootElement.EnumerateArray().ToArray();
    if (elements[0].GetInt32() != 3)
        throw new Exception($"FAILED: {action} answered with CALLERROR {reply}");
    return elements[2].Clone();
}
async Task<T> Query<T>(Func<VoltaXApiDbContext, Task<T>> query)
{
    await using var scope = provider.CreateAsyncScope();
    return await query(scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>());
}
var now = DateTime.UtcNow;

// Boot
var boot = await Call(Cp16, "BootNotification", new { chargePointVendor = "Acme", chargePointModel = "AC22", chargePointSerialNumber = "SN-16" });
Check(boot.GetProperty("status").GetString() == "Accepted" && boot.GetProperty("interval").GetInt32() == 300, "provisioned charger boots Accepted");
boot = await Call(CpPending, "BootNotification", new { chargePointVendor = "Acme", chargePointModel = "AC22" });
Check(boot.GetProperty("status").GetString() == "Pending" && boot.GetProperty("interval").GetInt32() == 60, "unprovisioned charger boots Pending");
boot = await Call("UNKNOWN", "BootNotification", new { chargePointVendor = "Acme", chargePointModel = "AC22" });
Check(boot.GetProperty("status").GetString() == "Rejected", "unknown charger boots Rejected");
Check(await Query(db => db.ChargePoints.AnyAsync(cp => cp.ChargePointId == Cp16 && cp.VendorName == "Acme" && cp.SerialNumber == "SN-16")), "boot information stored");

var heartbeat = await Call(Cp16, "Heartbeat", new { });
Check(heartbeat.TryGetProperty("currentTime", out _), "Heartbeat answers currentTime");

// StatusNotification
await Call(Cp16, "StatusNotification", new { connectorId = 0, errorCode = "NoError", status = "Available" });
await Call(Cp16, "StatusNotification", new { connectorId = 1, errorCode = "NoError", status = "Preparing", timestamp = now });
var connector = await Query(db => db.Connectors.SingleOrDefaultAsync(c => c.ChargePoint!.ChargePointId == Cp16));
Check(connector != null && connector.EvseID == 1 && connector.ConnectorID == 1 && connector.PricePerKWh == 2, "1.6 connector 1 registered as EVSE 1 with default pricing");
Check(await Query(db => db.ConnectorStatuses.AnyAsync(s => s.ConnectorID == connector!.ID && s.LastStatus == ConnectorStatusEnumType.Occupied)), "Preparing stored as Occupied");
await Query(async db =>
{
    var row = await db.Connectors.SingleAsync(c => c.ID == connector!.ID);
    row.PricePerMinute = 0.5;
    return await db.SaveChangesAsync();
});

// Authorize
string AuthStatus(JsonElement e) => e.GetProperty("idTagInfo").GetProperty("status").GetString()!;
Check(AuthStatus(await Call(Cp16, "Authorize", new { idTag = "TAG1" })) == "Accepted", "Authorize: active card Accepted");
Check(AuthStatus(await Call(Cp16, "Authorize", new { idTag = "BLOCKED" })) == "Blocked", "Authorize: blocked card Blocked");
Check(AuthStatus(await Call(Cp16, "Authorize", new { idTag = "NOPE" })) == "Invalid", "Authorize: unknown token Invalid");
Check(AuthStatus(await Call(CpPending, "Authorize", new { idTag = "TAG1" })) == "Invalid", "Authorize: refused while the charger is Pending");

// Start / MeterValues / Stop
var start = await Call(Cp16, "StartTransaction", new { connectorId = 1, idTag = "TAG1", meterStart = 1000, timestamp = now });
var transactionId = start.GetProperty("transactionId").GetInt32();
Check(AuthStatus(start) == "Accepted" && transactionId > 0, "StartTransaction accepted with an integer transactionId");
var uid = Ocpp16Transaction.ToUid(transactionId);
var transaction = await Query(db => db.Transactions.SingleOrDefaultAsync(t => t.Uid == uid));
Check(transaction != null && transaction.ConnectorID == connector!.ID && transaction.MeterStart == 1.0, "transaction stored with uid ocpp16-N and meterStart in kWh");
Check(await Query(db => db.ChargingSessions.AnyAsync(s => s.ID == transaction!.ChargingSessionID)), "charging session started");

await Call(Cp16, "MeterValues", new
{
    connectorId = 1, transactionId,
    meterValue = new[]
    {
        new
        {
            timestamp = now.AddMinutes(10),
            sampledValue = new object[]
            {
                new { value = "5.5", measurand = "Energy.Active.Import.Register", unit = "kWh", context = "Sample.Periodic" },
                new { value = "7400", measurand = "Power.Active.Import", unit = "W" },
                new { value = "1800", measurand = "Energy.Active.Import.Register", unit = "Wh", phase = "L1-N" }
            }
        }
    }
});
transaction = await Query(db => db.Transactions.SingleAsync(t => t.Uid == uid));
Check(transaction.MeterStop == 5.5, "MeterValues: kWh register (whole meter beats phase) updates the transaction");
Check(Connection(Cp16).Status.OnlineConnectors[1].ChargeRateKW == 7.4, "MeterValues: W converted to kW for the live view");

var balanceBefore = await Query(db => db.Cards.Where(c => c.CardNumber == "TAG1").Select(c => c.Balance).SingleAsync());
var stop = await Call(Cp16, "StopTransaction", new
{
    transactionId, idTag = "TAG1", meterStop = 13000, timestamp = now.AddMinutes(30), reason = "EVDisconnected",
    transactionData = new[] { new { timestamp = now.AddMinutes(30), sampledValue = new[] { new { value = "13000", context = "Transaction.End" } } } }
});
Check(AuthStatus(stop) == "Accepted", "StopTransaction answered with idTagInfo");
transaction = await Query(db => db.Transactions.SingleAsync(t => t.Uid == uid));
Check(transaction.Status == TransactionStatusEnum.Ended && transaction.MeterStop == 13.0 && transaction.StopReason == "EVDisconnected", "transaction ended on meterStop (Wh -> kWh) with the 1.6 reason");
var session = await Query(db => db.ChargingSessions.SingleAsync(s => s.ID == transaction.ChargingSessionID));
Check(Math.Abs((session.ChargedKwhs ?? 0) - 12.0) < 1e-9 && Math.Abs((session.ChargedMinutes ?? 0) - 30) < 1e-6, "session closed with 12 kWh over 30 minutes");
var balanceAfter = await Query(db => db.Cards.Where(c => c.CardNumber == "TAG1").Select(c => c.Balance).SingleAsync());
Check(Math.Abs(balanceBefore - balanceAfter - 15.0) < 1e-6, "card billed like 2.0.1 (30 min x 0.5)");
var stopAgain = await Call(Cp16, "StopTransaction", new { transactionId, meterStop = 14000, timestamp = now.AddMinutes(31) });
Check(!stopAgain.TryGetProperty("idTagInfo", out _), "a repeated StopTransaction is acknowledged without billing again");
Check(await Query(db => db.Cards.Where(c => c.CardNumber == "TAG1").Select(c => c.Balance).SingleAsync()) == balanceAfter, "no second debit");

// Refusals
start = await Call(Cp16, "StartTransaction", new { connectorId = 1, idTag = "NOPE", meterStart = 0, timestamp = now });
Check(AuthStatus(start) == "Invalid" && start.GetProperty("transactionId").GetInt32() > transactionId, "unknown idTag: Invalid, still a transactionId");
start = await Call(Cp16, "StartTransaction", new { connectorId = 1, idTag = "EMPTY", meterStart = 0, timestamp = now });
Check(AuthStatus(start) == "Invalid", "card without balance refused (NoCredit -> 1.6 Invalid)");
start = await Call(CpPending, "StartTransaction", new { connectorId = 1, idTag = "TAG1", meterStart = 0, timestamp = now });
Check(AuthStatus(start) == "Invalid", "transactions refused while the charger is not Accepted");
var refusedId = start.GetProperty("transactionId").GetInt32();
await Call(CpPending, "StopTransaction", new { transactionId = refusedId, meterStop = 500, timestamp = now.AddMinutes(1) });
Check(!await Query(db => db.Transactions.AnyAsync(t => t.Uid == Ocpp16Transaction.ToUid(refusedId))), "a refused transaction is never billed");

// Other inbound messages
var dataTransfer = await Call(Cp16, "DataTransfer", new { vendorId = "acme", messageId = "x", data = "y" });
Check(dataTransfer.GetProperty("status").GetString() == "Accepted", "DataTransfer acknowledged");
await Call(Cp16, "DiagnosticsStatusNotification", new { status = "Uploaded" });
await Call(Cp16, "FirmwareStatusNotification", new { status = "Installed" });
Check(await Query(db => db.MessageLogs.CountAsync()) > 10, "1.6 messages written to the message log");

// ---------------------------------------------------------------- CSMS -> charger routing
sender.Versions[Cp16] = OcppProtocols.Ocpp16;
sender.Versions[Cp201] = OcppProtocols.Ocpp201;
sender.Reply = action => action switch
{
    "GetConfiguration" => new { configurationKey = new[] { new { key = "HeartbeatInterval", @readonly = false, value = "300" } }, unknownKey = new[] { "Nope" } },
    "UpdateFirmware" => new { },
    _ => new { status = "Accepted" }
};
await using (var scope = provider.CreateAsyncScope())
{
    var evDriver = scope.ServiceProvider.GetRequiredService<IEVDriverService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

    var started = await evDriver.RequestStartTransaction(Cp16, new RequestStartTransactionRequest { IdToken = new IdTokenType { IdToken = "TAG1" }, EvseId = 1 });
    Check(sender.LastFrame == "[2,\"c1\",\"RemoteStartTransaction\",{\"connectorId\":1,\"idTag\":\"TAG1\"}]" && started.Status == RequestStartStopStatusEnum.Accepted,
        "RequestStartTransaction to a 1.6 charger sends RemoteStartTransaction");
    await evDriver.RequestStopTransaction(Cp16, new RequestStopTransactionRequest { TransactionId = uid });
    Check(sender.LastAction == "RemoteStopTransaction" && sender.LastPayload == $"{{\"transactionId\":{transactionId}}}", "RequestStopTransaction sends RemoteStopTransaction with the integer id");
    await evDriver.RequestStartTransaction(Cp201, new RequestStartTransactionRequest { IdToken = new IdTokenType { IdToken = "TAG1" }, EvseId = 1, RemoteStartId = 5 });
    Check(sender.LastAction == "RequestStartTransaction", "a 2.0.1 charger still gets RequestStartTransaction");

    await configuration.Reset(Cp16, new ResetRequest { Type = ResetEnumType.Immediate });
    Check(sender.LastAction == "Reset" && sender.LastPayload == "{\"type\":\"Hard\"}", "Reset Immediate -> 1.6 Hard");
    await configuration.ChangeAvailability(Cp16, new ChangeAvailabilityRequest { Evse = new EVSEType { Id = 1 }, OperationalStatus = OperationalStatusEnumType.Inoperative });
    Check(sender.LastPayload == "{\"connectorId\":1,\"type\":\"Inoperative\"}", "ChangeAvailability -> connectorId/type");
    await configuration.TriggerMessage(Cp16, new TriggerMessageRequest { RequestedMessage = MessageTriggerEnumType.StatusNotification });
    Check(sender.LastPayload == "{\"requestedMessage\":\"StatusNotification\"}", "TriggerMessage mapped");
    await evDriver.UnlockConnector(Cp16, new UnlockConnectorRequest { EvseId = 1, ConnectorId = 1 });
    Check(sender.LastAction == "UnlockConnector" && sender.LastPayload == "{\"connectorId\":1}", "UnlockConnector uses the 1.6 connectorId");
    var firmware = await configuration.UpdateFirmware(Cp16, new UpdateFirmwareRequest { RequestId = 1, Firmware = new FirmwareType { Location = "https://fw/x.bin", RetrieveDateTime = "2026-10-05T10:00:00Z" } });
    Check(sender.LastPayload == "{\"location\":\"https://fw/x.bin\",\"retrieveDate\":\"2026-10-05T10:00:00Z\"}" && firmware.Status == UpdateFirmwareStatusEnumType.Accepted, "UpdateFirmware -> location/retrieveDate");
    var keys = await configuration.GetConfiguration(Cp16, new GetConfigurationDto { Keys = new List<string> { "HeartbeatInterval" } });
    Check(sender.LastPayload == "{\"key\":[\"HeartbeatInterval\"]}" && keys.ConfigurationKey!.Single().Value == "300", "GetConfiguration round trip");
    var changed = await configuration.ChangeConfiguration(Cp16, new ChangeConfigurationDto { Key = "HeartbeatInterval", Value = "120" });
    Check(changed.Status == "Accepted" && sender.LastPayload == "{\"key\":\"HeartbeatInterval\",\"value\":\"120\"}", "ChangeConfiguration round trip");

    var before = sender.Count;
    try { await configuration.ChangeConfiguration(Cp201, new ChangeConfigurationDto { Key = "A", Value = "B" }); Check(false, "ChangeConfiguration on 2.0.1 refused"); }
    catch (OcppProtocolNotSupportedException ex) { Check(ex.Message.Contains("2.0.1") && sender.Count == before, "ChangeConfiguration on a 2.0.1 charger -> not supported, nothing sent"); }
    try { await configuration.SetDisplayMessage(Cp16, new SetDisplayMessageRequest()); Check(false, "SetDisplayMessage on 1.6 refused"); }
    catch (OcppProtocolNotSupportedException ex) { Check(ex.Message.Contains("1.6") && sender.Count == before, "2.0.1-only command on a 1.6 charger -> not supported, nothing sent"); }
    var error = VoltaXApi.OCPP.Controllers.OcppCommandResult.ToError(new OcppProtocolNotSupportedException("x")) as Microsoft.AspNetCore.Mvc.ObjectResult;
    Check(error?.StatusCode == 400, "not supported -> HTTP 400");

    // Reservations
    var reserved = await evDriver.ReserveNow(Cp16, new ReserveNowRequest { Id = 0, EvseId = 1, ExpiryDateTime = DateTime.UtcNow.AddMinutes(15), IdToken = new IdTokenType { IdToken = "TAG1" } });
    var reservation = await Query(db => db.Reservations.SingleAsync());
    Check(reserved.Status == ReserveNowStatusEnumType.Accepted && reservation.Status == ReservationStatusEnum.Active && reservation.ReservationId == reservation.ID
          && reservation.ConnectorID == connector!.ID && reservation.UserID != null, "ReserveNow persisted (id allocated, connector and user resolved)");
    Check(sender.LastAction == "ReserveNow" && sender.LastPayload!.Contains($"\"reservationId\":{reservation.ReservationId}") && sender.LastPayload.Contains("\"connectorId\":1"), "ReserveNow sent in 1.6 form");
    start = await Call(Cp16, "StartTransaction", new { connectorId = 1, idTag = "TAG1", meterStart = 13000, reservationId = reservation.ReservationId, timestamp = DateTime.UtcNow });
    reservation = await Query(db => db.Reservations.SingleAsync());
    Check(reservation.Status == ReservationStatusEnum.Used && reservation.TransactionUid == Ocpp16Transaction.ToUid(start.GetProperty("transactionId").GetInt32()), "StartTransaction with reservationId marks it Used");

    var second = await evDriver.ReserveNow(Cp16, new ReserveNowRequest { Id = 77, EvseId = 1, ExpiryDateTime = DateTime.UtcNow.AddSeconds(-1), IdToken = new IdTokenType { IdToken = "TAG1" } });
    var expired = await scope.ServiceProvider.GetRequiredService<ReservationService>().ExpireDueAsync(DateTime.UtcNow);
    Check(expired == 1 && await Query(db => db.Reservations.AnyAsync(r => r.ReservationId == 77 && r.Status == ReservationStatusEnum.Expired)), "expired reservations swept");
    await evDriver.ReserveNow(Cp16, new ReserveNowRequest { Id = 78, EvseId = 1, ExpiryDateTime = DateTime.UtcNow.AddMinutes(5), IdToken = new IdTokenType { IdToken = "TAG1" } });
    await evDriver.CancelReservation(Cp16, new CancelReservationRequest { ReservationId = 78 });
    Check(sender.LastPayload == "{\"reservationId\":78}" && await Query(db => db.Reservations.AnyAsync(r => r.ReservationId == 78 && r.Status == ReservationStatusEnum.Cancelled)), "CancelReservation sent and recorded");
}

// 2.0.1 ReservationStatusUpdate
await using (var scope = provider.CreateAsyncScope())
{
    var evDriver = scope.ServiceProvider.GetRequiredService<IEVDriverService>();
    await evDriver.ReserveNow(Cp201, new ReserveNowRequest { Id = 90, EvseId = 1, ExpiryDateTime = DateTime.UtcNow.AddMinutes(5), IdToken = new IdTokenType { IdToken = "TAG1" } });
}
await Call(Cp201, "ReservationStatusUpdate", new { reservationId = 90, reservationUpdateStatus = "Removed" }, OcppProtocols.Ocpp201);
Check(await Query(db => db.Reservations.AnyAsync(r => r.ReservationId == 90 && r.Status == ReservationStatusEnum.Cancelled)), "2.0.1 ReservationStatusUpdate Removed -> Cancelled");

// ---------------------------------------------------------------- 2.0.1 Started without EVSE
object Event201(string type, string trigger, object? evse, string? idToken, double? wh, DateTime at, int? reservationId = null) => new
{
    eventType = type, triggerReason = trigger, timestamp = at.ToString("o"), seqNo = 0,
    transactionInfo = new { transactionId = "TX-201" },
    evse, reservationId,
    idToken = idToken == null ? null : new { idToken, type = "ISO14443" },
    meterValue = wh == null ? null : new[] { new { timestamp = at, sampledValue = new[] { new { value = wh, measurand = "Energy.Active.Import.Register" } } } }
};
await Call(Cp201, "StatusNotification", new { timestamp = now.ToString("o"), connectorStatus = "Occupied", evseId = 1, connectorId = 1 }, OcppProtocols.Ocpp201);
var started201 = await Call(Cp201, "TransactionEvent", Event201("Started", "Authorized", null, "TAG1", 2000, now), OcppProtocols.Ocpp201);
Check(started201.GetProperty("idTokenInfo").GetProperty("status").GetString() == "Accepted", "2.0.1 Started without EVSE is accepted");
Check(await Query(db => db.OcppPendingTransactions.AnyAsync(p => p.TransactionUid == "TX-201")) && !await Query(db => db.Transactions.AnyAsync(t => t.Uid == "TX-201")),
    "it waits for its EVSE without a session");
await Call(Cp201, "TransactionEvent", Event201("Updated", "CablePluggedIn", new { id = 1, connectorId = 1 }, null, 2500, now.AddMinutes(1)), OcppProtocols.Ocpp201);
var tx201 = await Query(db => db.Transactions.SingleOrDefaultAsync(t => t.Uid == "TX-201"));
Check(tx201 != null && tx201.MeterStart == 2.0 && tx201.StartTime == now && tx201.MeterStop == 2.5 && !await Query(db => db.OcppPendingTransactions.AnyAsync()),
    "the event carrying the EVSE starts it with the Started data, then applies itself");
await Call(Cp201, "TransactionEvent", Event201("Ended", "EVDeparted", null, null, 4000, now.AddMinutes(20)), OcppProtocols.Ocpp201);
tx201 = await Query(db => db.Transactions.SingleAsync(t => t.Uid == "TX-201"));
Check(tx201.Status == TransactionStatusEnum.Ended && tx201.MeterStop == 4.0, "Ended without EVSE closes it through the known connector");

Console.WriteLine($"All {checks} OCPP 1.6 checks passed.");

// ---------------------------------------------------------------- fakes
sealed class FakeCommandSender : IOcppCommandSender
{
    public Dictionary<string, string> Versions { get; } = new();
    public Func<string, object> Reply { get; set; } = _ => new { status = "Accepted" };
    public int Count { get; private set; }
    public string? LastAction { get; private set; }
    public string? LastPayload { get; private set; }
    public string? LastFrame { get; private set; }

    public string? GetProtocolVersion(string chargePointId) => Versions.TryGetValue(chargePointId, out var v) ? v : null;

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(string chargePointId, string action, TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        Count++;
        LastAction = action;
        LastPayload = JsonConvert.SerializeObject(request, OCPPMessageFactory.DefaultSettings);
        LastFrame = OcppJson.SerializeCall($"c{Count}", action, LastPayload);
        var reply = JsonConvert.SerializeObject(Reply(action), OCPPMessageFactory.DefaultSettings);
        return Task.FromResult(JsonConvert.DeserializeObject<TResponse>(reply, OCPPMessageFactory.DefaultSettings)!);
    }
}

sealed class NoExternalTokens : IExternalTokenAuthorizer
{
    public Task<AuthorizationStatusEnumType?> AuthorizeAsync(string? idToken, string? chargePointId, CancellationToken cancellationToken = default) =>
        Task.FromResult<AuthorizationStatusEnumType?>(null);
}

sealed class NoRoaming : IRoamingTransactionObserver
{
    public Task<AuthorizationStatusEnumType?> OnTransactionEventAsync(string chargePointId, TransactionEventRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<AuthorizationStatusEnumType?>(null);
}

/// <summary>Interface stub: every method returns default / a completed task.</summary>
class Stub<T> : DispatchProxy where T : class
{
    public static T Create() => DispatchProxy.Create<T, Stub<T>>();

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        var type = method!.ReturnType;
        if (type == typeof(Task)) return Task.CompletedTask;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var result = type.GetGenericArguments()[0];
            var value = result.IsValueType ? Activator.CreateInstance(result) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(result).Invoke(null, new[] { value });
        }
        return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
    }
}

sealed class IdleWebSocket : WebSocket
{
    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => WebSocketState.Open;
    public override string? SubProtocol => "ocpp1.6";
    public override void Abort() { }
    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
    public override void Dispose() { }
    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();
    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) => Task.CompletedTask;
}

sealed class NoLoadBalancing : VoltaXApi.SmartCharging.ILoadBalancingTrigger
{
    public void RequestRebalance(int chargingStationId) { }
    public void RequestRebalanceForChargePoint(string chargePointId) { }
    public Task<VoltaXApi.SmartCharging.RebalanceSummary> RebalanceNowAsync(int chargingStationId, string reason, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
