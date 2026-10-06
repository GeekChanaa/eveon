using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using VoltaXApi.Models;
using VoltaXApi.Models.Ocpp201;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
async Task Throws<TException>(Func<Task> action, string name) where TException : Exception
{
    try { await action(); }
    catch (TException) { Check(true, name); return; }
    catch (Exception ex) { throw new Exception($"FAILED: {name} (threw {ex.GetType().Name}: {ex.Message})"); }
    throw new Exception("FAILED (no exception): " + name);
}

// ---------------------------------------------------------------- NotifyEvent
var eventPart0 = JsonConvert.DeserializeObject<NotifyEventRequest>(@"{
  ""generatedAt"": ""2026-10-03T10:00:00+02:00"", ""tbc"": true, ""seqNo"": 0,
  ""eventData"": [
    { ""eventId"": 1, ""timestamp"": ""2026-10-03T09:59:00+02:00"", ""trigger"": ""Alerting"", ""actualValue"": ""71"",
      ""eventNotificationType"": ""CustomMonitor"", ""component"": { ""name"": ""EVSE"", ""evse"": { ""id"": 1, ""connectorId"": 2 } },
      ""variable"": { ""name"": ""Temperature"" }, ""variableMonitoringId"": 7, ""techCode"": ""T-01"" },
    { ""eventId"": 2, ""timestamp"": ""2026-10-03T08:00:00Z"", ""trigger"": ""Delta"", ""actualValue"": ""true"",
      ""eventNotificationType"": ""HardWiredNotification"", ""component"": { ""name"": ""ChargingStation"" }, ""variable"": { ""name"": ""Problem"" } }
  ]}")!;
var eventPart1 = JsonConvert.DeserializeObject<NotifyEventRequest>(@"{
  ""generatedAt"": ""2026-10-03T08:00:05Z"", ""tbc"": false, ""seqNo"": 1,
  ""eventData"": [
    { ""eventId"": 3, ""timestamp"": ""2026-10-03T08:00:04Z"", ""trigger"": ""Delta"", ""actualValue"": ""false"", ""cleared"": true,
      ""eventNotificationType"": ""HardWiredNotification"", ""component"": { ""name"": ""ChargingStation"" }, ""variable"": { ""name"": ""Problem"" } },
    { ""eventId"": 4, ""timestamp"": ""2026-10-03T08:00:04Z"", ""trigger"": ""Periodic"", ""actualValue"": ""230.1"",
      ""eventNotificationType"": ""PreconfiguredMonitor"", ""component"": { ""name"": ""EVSE"", ""evse"": { ""id"": 1 } },
      ""variable"": { ""name"": ""Voltage"" }, ""variableMonitoringId"": 8 }
  ]}")!;
var severities = new Dictionary<int, int> { [7] = 2, [8] = 8 };
var received = new DateTime(2026, 10, 3, 8, 1, 0, DateTimeKind.Utc);
var events = ChargerEventMapper.Map("CP-1", eventPart0, received, severities).Concat(ChargerEventMapper.Map("CP-1", eventPart1, received, severities)).ToList();
Check(events.Count == 4 && events.Select(e => e.EventId).SequenceEqual(new[] { 1, 2, 3, 4 }), "NotifyEvent: every part (tbc/seqNo) stored, all eventData mapped");
Check(events[0].Timestamp == new DateTime(2026, 10, 3, 7, 59, 0, DateTimeKind.Utc) && events[0].Timestamp.Kind == DateTimeKind.Utc, "NotifyEvent: timestamp with offset converted to UTC");
Check(events[0].EvseId == 1 && events[0].ConnectorId == 2 && events[0].ComponentName == "EVSE" && events[0].VariableName == "Temperature"
      && events[0].Trigger == "Alerting" && events[0].EventNotificationType == "CustomMonitor" && events[0].TechCode == "T-01", "NotifyEvent: component/variable/evse/trigger mapped");
Check(events[0].Severity == 2 && events[1].Severity == null && events[3].Severity == 8, "NotifyEvent: severity taken from the reporting monitor");
Check(ChargerEventMapper.IsAlarm(events[0]), "alarm: monitor severity 2 (<= 3)");
Check(ChargerEventMapper.IsAlarm(events[1]), "alarm: hard-wired Problem=true");
Check(!ChargerEventMapper.IsAlarm(events[2]), "no alarm: cleared Problem event");
Check(!ChargerEventMapper.IsAlarm(events[3]), "no alarm: severity 8 periodic voltage");
var longEvent = new NotifyEventRequest { EventData = new() { new EventDataType { TechInfo = new string('x', 900), Component = new ComponentType { Name = "C" }, Variable = new VariableType { Name = "V" } } }, GeneratedAt = received };
Check(ChargerEventMapper.Map("CP-1", longEvent, received)[0].TechInfo!.Length == 500, "NotifyEvent: oversized fields truncated to column size");

// ---------------------------------------------------------------- NotifyMonitoringReport multi-part
NotifyMonitoringReportRequest MonitorPart(int requestId, int seq, bool tbc, params (int Id, string Variable, int Severity)[] monitors) => new()
{
    RequestId = requestId, SeqNo = seq, Tbc = tbc, GeneratedAt = received,
    Monitor = monitors.Select(m => new MonitoringDataType
    {
        Component = new ComponentType { Name = "EVSE", Evse = new EVSEType { Id = 1 } },
        Variable = new VariableType { Name = m.Variable },
        VariableMonitoring = new() { new VariableMonitoringType { Id = m.Id, Severity = m.Severity, Type = MonitorEnumType.UpperThreshold, Value = 60 } }
    }).ToList()
};
using (var assembler = new MonitoringReportAssembler())
{
    Check(assembler.Add("CP-1", MonitorPart(5, 1, true, (2, "Power", 4))) == null, "monitoring report: part 1 alone is incomplete");
    Check(assembler.Add("CP-1", MonitorPart(5, 2, false, (3, "Current", 5))) == null, "monitoring report: last part with a gap (seq 0 missing) is incomplete");
    Check(assembler.Add("CP-2", MonitorPart(5, 0, false, (9, "Power", 1)))!.Count == 1, "monitoring report: other charger with same requestId is independent");
    var complete = assembler.Add("CP-1", MonitorPart(5, 0, true, (1, "Temperature", 2), (2, "Power", 4)));
    Check(complete != null && complete.Count == 4, "monitoring report: complete once parts 0..last arrived (out of order)");
    var rows = ChargerEventMapper.MapMonitors("CP-1", 5, complete!, received);
    Check(rows.Count == 3 && rows.Select(r => r.MonitoringId).OrderBy(i => i).SequenceEqual(new[] { 1, 2, 3 }), "monitoring report: monitors deduplicated by monitoring id");
    Check(rows.Single(r => r.MonitoringId == 1) is { Severity: 2, Type: "UpperThreshold", Value: 60, EvseId: 1, VariableName: "Temperature", RequestId: 5 }, "monitoring report: monitor fields mapped");
    Check(assembler.Add("CP-1", MonitorPart(5, 0, false, (1, "Temperature", 2)))!.Count == 1, "monitoring report: a completed report starts over with the same requestId");
    Check(assembler.Add("CP-1", MonitorPart(6, 0, false))!.Count == 0, "monitoring report: single empty part completes");
    assembler.MarkFullReport("CP-1", 7);
    Check(assembler.IsFullReport("CP-1", 7) && !assembler.IsFullReport("CP-1", 8) && !assembler.IsFullReport("CP-2", 7), "monitoring report: full (unfiltered) report flag per charger and request");
    await Throws<ArgumentException>(() => Task.FromResult(assembler.Add("CP-1", MonitorPart(9, -1, false))), "monitoring report: negative seqNo rejected");
}
using (var expiring = new MonitoringReportAssembler(TimeSpan.FromMilliseconds(50)))
{
    expiring.Add("CP-1", MonitorPart(1, 0, true, (1, "Power", 4)));
    await Task.Delay(120);
    Check(expiring.Add("CP-1", MonitorPart(1, 1, false, (2, "Power", 4))) == null, "monitoring report: stale parts expire (no partial report assembled)");
}

// ---------------------------------------------------------------- NotifyCustomerInformation multi-part
var ci = CustomerInformationAssembler.Append(null, 1, "world", tbc: false);
Check(!ci.Complete && ci.PartsReceived == 1, "customer information: last part before the first is incomplete");
ci = CustomerInformationAssembler.Append(ci.PartsJson, 0, "hello ", tbc: true);
Check(ci.Complete && ci.Data == "hello world" && ci.PartsReceived == 2, "customer information: parts assembled in seqNo order");
var single = CustomerInformationAssembler.Append(null, 0, "only", tbc: false);
Check(single.Complete && single.Data == "only", "customer information: single part complete");
var gap = CustomerInformationAssembler.Append(CustomerInformationAssembler.Append(null, 0, "a", true).PartsJson, 2, "c", false);
Check(!gap.Complete, "customer information: missing middle part keeps it incomplete");
Check(CustomerInformationAssembler.Append(gap.PartsJson, 1, "b", true) is { Complete: true, Data: "abc" }, "customer information: completes when the gap is filled");

// ---------------------------------------------------------------- display messages
var display = JsonConvert.DeserializeObject<NotifyDisplayMessagesRequestType>(@"{ ""requestId"": 12, ""tbc"": false, ""messageInfo"": [
  { ""id"": 3, ""priority"": ""InFront"", ""state"": ""Charging"", ""startDateTime"": ""2026-10-03T10:00:00+02:00"", ""transactionId"": ""tx-1"",
    ""message"": { ""format"": ""UTF8"", ""language"": ""fr"", ""content"": ""Bonjour"" }, ""display"": { ""name"": ""Display"", ""evse"": { ""id"": 2 } } } ] }")!;
var snapshot = ChargerEventMapper.MapDisplayMessages("CP-1", display.RequestId!.Value, display.MessageInfo, received).Single();
Check(snapshot is { RequestId: 12, MessageId: 3, Priority: "InFront", State: "Charging", Content: "Bonjour", Format: "UTF8", Language: "fr", DisplayComponentName: "Display", DisplayEvseId: 2, TransactionId: "tx-1" }
      && snapshot.StartDateTime == new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc), "display messages: snapshot fields mapped, dates in UTC");

// ---------------------------------------------------------------- log upload tickets
var token = LogUploadTickets.NewToken();
Check(token.Length == 43 && !token.Contains('/') && !token.Contains('+') && !token.Contains('='), "upload ticket: 256-bit token, URL-safe base64");
Check(LogUploadTickets.NewToken() != token, "upload ticket: tokens are random");
var hash = LogUploadTickets.Hash(token);
Check(hash.Length == 64 && hash == LogUploadTickets.Hash(token) && hash != LogUploadTickets.Hash(token + "x") && !hash.Contains(token), "upload ticket: stored as SHA-256 hex, never in clear");
var now = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
var ticket = new LogUploadTicket { TokenHash = hash, ChargePointID = "CP-1", RequestId = 4, CreatedAt = now, ExpiresAt = now.AddHours(2) };
Check(LogUploadTickets.Validate(ticket, now.AddMinutes(5)) == LogUploadRejection.None, "upload ticket: valid before expiry");
Check(LogUploadTickets.Validate(ticket, now.AddHours(2)) == LogUploadRejection.Expired, "upload ticket: expired at expiry time");
Check(LogUploadTickets.Validate(null, now) == LogUploadRejection.UnknownTicket, "upload ticket: unknown token refused");
ticket.UsedAt = now.AddMinutes(1);
Check(LogUploadTickets.Validate(ticket, now.AddMinutes(2)) == LogUploadRejection.AlreadyUsed, "upload ticket: single use");
Check(LogUploadTickets.SafeSegment("../../etc") == "_.._etc" && !LogUploadTickets.SafeSegment("a/b\\c").Contains('/') && LogUploadTickets.SafeSegment("..") == "_", "upload storage: charger id cannot traverse directories");
Check(LogUploadTickets.SafeExtension("diag.TAR") == "tar" && LogUploadTickets.SafeExtension("x.ex e") == "log" && LogUploadTickets.SafeExtension(null) == "log" && LogUploadTickets.SafeExtension("a.verylongextension") == "log", "upload storage: extension whitelisted");
Check(LogUploadTickets.SafeFileName("../../secret.zip", "f") == "secret.zip" && LogUploadTickets.SafeFileName("", "fallback.log") == "fallback.log", "upload storage: file name stripped of path");

var root = Path.Combine(Path.GetTempPath(), "voltax-logchecks-" + Guid.NewGuid().ToString("N"));
try
{
    var storage = new ChargerLogStorage(root);
    var payload = System.Text.Encoding.UTF8.GetBytes("line 1\nline 2\n");
    var stored = await storage.SaveAsync(new MemoryStream(payload), "CP/1", "log", 1024, CancellationToken.None);
    var expectedSha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload)).ToLowerInvariant();
    Check(stored.SizeBytes == payload.Length && stored.Sha256 == expectedSha && File.ReadAllBytes(storage.FullPath(stored.RelativePath)).SequenceEqual(payload),
        "upload storage: size and SHA-256 recorded, content stored");
    Check(storage.FullPath(stored.RelativePath).StartsWith(Path.GetFullPath(root)) && stored.RelativePath.StartsWith("CP_1"), "upload storage: file under {root}/{chargePointId}/");
    await Throws<LogTooLargeException>(() => storage.SaveAsync(new MemoryStream(new byte[2048]), "CP-1", "log", 1024, CancellationToken.None), "upload storage: size limit enforced");
    Check(!Directory.Exists(Path.Combine(root, "CP-1")) || Directory.GetFiles(Path.Combine(root, "CP-1")).Length == 0, "upload storage: oversized partial file deleted");
    Check(ThrowsSync(() => storage.FullPath("../outside.log")), "upload storage: paths outside the root refused");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
}

// ---------------------------------------------------------------- CostUpdated cost = billing
var connector = new Connector { PricePerKWh = 3.2, PricePerMinute = 0.45, PricePerIdleMinute = 0.1 };
var calculator = CostCalculator.Instance;
foreach (var (kwh, minutes) in new[] { (0.0, 0.0), (12.5, 30.0), (40.0, 10.0), (1.0, 300.0) })
{
    // The formulas TransactionService (running cost) and CardService (debit) used before the extraction.
    var billingRunning = Math.Max(kwh * connector.PricePerKWh, minutes * connector.PricePerMinute);
    Check(calculator.RunningCost(connector, kwh, minutes) == billingRunning, $"cost: running cost {kwh} kWh / {minutes} min equals billing");
    Check(calculator.FinalCost(connector, minutes) == minutes * connector.PricePerMinute, $"cost: final cost {minutes} min equals the card debit");
}
Check(calculator.EnergyCost(connector, 10) == 10 * 3.2 && calculator.IdleCost(connector, 10) == 10 * 0.1, "cost: energy and idle prices");
var start = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);
var active = new CostUpdatedHostedService.ActiveTransaction("CP-1", 1, "tx-1", start, 1000, 1012.5, connector);
Check(CostUpdatedHostedService.RunningCost(calculator, active, start.AddMinutes(30)) == calculator.RunningCost(connector, 12.5, 30), "CostUpdated: running cost of an active transaction uses the billing formula");
Check(CostUpdatedHostedService.RunningCost(calculator, active with { MeterStop = null }, start.AddMinutes(-5)) == 0, "CostUpdated: no meter value / clock skew never gives a negative cost");
Check(CostUpdatedSender.RoundCost(12.345) == 12.35 && CostUpdatedSender.RoundCost(-1) == 0, "CostUpdated: totalCost rounded to cents");

var fake = new FakeSender();
var sender = new CostUpdatedSender(fake, NullLogger<CostUpdatedSender>.Instance);
Check(await sender.SendAsync("CP-1", "tx-1", 4.567) && fake.Sent.Single() == ("CP-1", "CostUpdated", 4.57, "tx-1"), "CostUpdated: sent to a 2.0.1 charger with {totalCost, transactionId}");
fake.Protocols["CP-16"] = OcppProtocols.Ocpp16;
Check(!await sender.SendAsync("CP-16", "tx-2", 1) && fake.Sent.Count == 1, "CostUpdated: never sent to a 1.6 charger");
Check(!await sender.SendAsync("CP-OFF", "tx-3", 1) && fake.Sent.Count == 1, "CostUpdated: offline charger skipped");
fake.Protocols["CP-NO"] = OcppProtocols.Ocpp201;
fake.Reject["CP-NO"] = "NotImplemented";
Check(!await sender.SendAsync("CP-NO", "tx-4", 1) && sender.IsSuppressed("CP-NO"), "CostUpdated: a charger that answers NotImplemented is remembered");
var calls = fake.Calls;
Check(!await sender.SendAsync("CP-NO", "tx-4", 2) && fake.Calls == calls, "CostUpdated: not sent again to a charger that rejected it");
fake.Reject["CP-1"] = "InternalError";
await Throws<OcppCallErrorException>(() => sender.SendAsync("CP-1", "tx-1", 1), "CostUpdated: other CALLERRORs surface to the caller");
Check(!sender.IsSuppressed("CP-1"), "CostUpdated: a transient error does not suppress the charger");

Console.WriteLine($"All {checks} checks passed.");

static bool ThrowsSync(Action action)
{
    try { action(); return false; }
    catch (InvalidOperationException) { return true; }
}

sealed class FakeSender : IOcppCommandSender
{
    public Dictionary<string, string> Protocols { get; } = new() { ["CP-1"] = OcppProtocols.Ocpp201 };
    public Dictionary<string, string> Reject { get; } = new();
    public List<(string ChargePointId, string Action, double TotalCost, string TransactionId)> Sent { get; } = new();
    public int Calls { get; private set; }

    public string? GetProtocolVersion(string chargePointId) => Protocols.TryGetValue(chargePointId, out var p) ? p : null;

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(string chargePointId, string action, TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        if (Reject.TryGetValue(chargePointId, out var code)) throw new OcppCallErrorException(code, null);
        var cost = (CostUpdatedRequest)(object)request!;
        Sent.Add((chargePointId, action, cost.TotalCost, cost.TransactionId));
        return Task.FromResult((TResponse)(object)new CostUpdatedResponse());
    }
}
