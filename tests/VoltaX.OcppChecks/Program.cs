using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCPP.Core.Server;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Helpers;
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
async Task Throws<TException>(Func<Task> action, string name, Func<TException, bool>? verify = null) where TException : Exception
{
    try { await action(); }
    catch (TException ex) { Check(verify?.Invoke(ex) ?? true, name); return; }
    catch (Exception ex) { throw new Exception($"FAILED: {name} (threw {ex.GetType().Name}: {ex.Message})"); }
    throw new Exception("FAILED (no exception): " + name);
}
JsonElement[] Elements(string frame) => JsonDocument.Parse(frame).RootElement.EnumerateArray().ToArray();

// ---------------------------------------------------------------- framing
Check(OcppJson.SerializeCall("abc", "Reset", "{\"type\":\"OnIdle\"}") == "[2,\"abc\",\"Reset\",{\"type\":\"OnIdle\"}]", "CALL is [2,id,action,payload]");
Check(OcppJson.SerializeCallResult("abc", "{\"status\":\"Accepted\"}") == "[3,\"abc\",{\"status\":\"Accepted\"}]", "CALLRESULT is [3,id,payload] without action");
Check(OcppJson.SerializeCallResult("abc", null) == "[3,\"abc\",{}]", "empty CALLRESULT payload is {}");
Check(OcppJson.SerializeCallError("abc", "NotImplemented", "Unknown action") == "[4,\"abc\",\"NotImplemented\",\"Unknown action\",{}]", "CALLERROR is [4,id,code,description,{}]");
Check(Elements(OcppJson.SerializeCallError("a\"b", "GenericError", "quote \" and \\ backslash")).Length == 5, "CALLERROR strings are escaped");

var call = OcppJson.Parse(" [ 2 , \"id-1\" , \"Heartbeat\" , { } ] ");
Check(call.Success && call.Frame!.MessageType == OcppMessageType.Call && call.Frame.UniqueId == "id-1" && call.Frame.Action == "Heartbeat" && call.Frame.Payload == "{ }", "CALL parsed (whitespace tolerated)");
var nested = OcppJson.Parse("[2,\"id-2\",\"DataTransfer\",{\"data\":\"[3,\\\"x\\\",{}]\",\"a\":[1,{\"b\":2}]}]");
Check(nested.Success && JsonDocument.Parse(nested.Frame!.Payload).RootElement.GetProperty("a")[1].GetProperty("b").GetInt32() == 2, "CALL payload with nested arrays/escaped brackets parsed");
var result = OcppJson.Parse("[3,\"id-3\",{\"status\":\"Accepted\"}]");
Check(result.Success && result.Frame!.MessageType == OcppMessageType.CallResult && result.Frame.Action == null && result.Frame.Payload.Contains("Accepted"), "CALLRESULT parsed");
var error = OcppJson.Parse("[4,\"id-4\",\"NotSupported\",\"nope\",{\"x\":1}]");
Check(error.Success && error.Frame!.ErrorCode == "NotSupported" && error.Frame.ErrorDescription == "nope" && error.Frame.Payload == "{\"x\":1}", "CALLERROR parsed");
Check(OcppJson.Parse("[4,\"id-5\",\"GenericError\",\"\"]").Success, "CALLERROR without details tolerated");

void Malformed(string text, string? id, OcppError code, string name)
{
    var parsed = OcppJson.Parse(text);
    Check(!parsed.Success && parsed.UniqueId == id && parsed.Error == code, name);
}
Malformed("not json", null, OcppError.RpcFrameworkError, "invalid JSON: no id");
Malformed("{\"a\":1}", null, OcppError.RpcFrameworkError, "JSON object instead of array: no id");
Malformed("[2]", null, OcppError.RpcFrameworkError, "too short without id");
Malformed("[2,\"id\"]", "id", OcppError.RpcFrameworkError, "too short with id");
Malformed("[\"2\",\"id\",\"Heartbeat\",{}]", "id", OcppError.RpcFrameworkError, "message type as string");
Malformed("[2,5,\"Heartbeat\",{}]", null, OcppError.RpcFrameworkError, "numeric message id is unreadable");
Malformed("[2,\"id\",\"Heartbeat\",[]]", "id", OcppError.FormationViolation, "CALL payload not an object");
Malformed("[2,\"id\",\"Heartbeat\"]", "id", OcppError.RpcFrameworkError, "CALL without payload");
Malformed("[2,\"id\",7,{}]", "id", OcppError.RpcFrameworkError, "CALL action not a string");
Malformed("[9,\"id\",{}]", "id", OcppError.MessageTypeNotSupported, "unknown message type");
Malformed("[2,\"" + new string('x', 37) + "\",\"Heartbeat\",{}]", new string('x', 37), OcppError.RpcFrameworkError, "message id longer than 36");
Malformed("[3,\"id\",\"Heartbeat\",{}]", "id", OcppError.RpcFrameworkError, "CALLRESULT with action is rejected");

Check(OcppErrors.ToWireName(OcppError.FormationViolation, OcppProtocols.Ocpp201) == "FormatViolation", "2.0.1 wire name FormatViolation");
Check(OcppErrors.ToWireName(OcppError.FormationViolation, OcppProtocols.Ocpp16) == "FormationViolation", "1.6 wire name FormationViolation");
Check(OcppErrors.ToWireName(OcppError.OccurrenceConstraintViolation, OcppProtocols.Ocpp16) == "OccurenceConstraintViolation", "1.6 wire name OccurenceConstraintViolation");
Check(OcppErrors.ToWireName(OcppError.RpcFrameworkError, OcppProtocols.Ocpp201) == "RpcFrameworkError", "2.0.1 RpcFrameworkError");
Check(OcppErrors.Parse("FormatViolation") == OcppError.FormationViolation && OcppErrors.Parse(ErrorCodes.OccurenceConstraintViolation) == OcppError.OccurrenceConstraintViolation
      && OcppErrors.Parse("Bogus") == OcppError.GenericError, "error codes parsed in both spellings, unknown -> GenericError");

// ---------------------------------------------------------------- services
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Ocpp:CommandTimeoutSeconds"] = "2",
    ["Ocpp:MaxMessageSizeBytes"] = "8192"
}).Build();
var services = new ServiceCollection();
services.AddLogging(b => b.SetMinimumLevel(LogLevel.Critical));
services.AddSingleton<IConfiguration>(config);
services.AddSingleton<WebSocketManagerService>();
services.AddSingleton<OcppPendingRequestRegistry>();
services.AddSingleton<OcppInboundHandlerRegistry>();
services.AddSingleton<OCPPRequestHandler>();
services.AddSingleton<OCPPMessageProcessor>();
services.AddSingleton<IOcppCommandSender>(sp => sp.GetRequiredService<OCPPMessageProcessor>());
services.AddSingleton<IHubContext<ChargerHub>, FakeHubContext>();
services.AddSingleton<ChargePointStatusManagerService>();
services.AddSingleton<ChargePointConnectivityNotifier>();
services.AddSingleton<WebSocketHandler>();
services.AddSingleton<WebSocketSubProtocolMatcher>();
services.AddScoped<ScopeMarker>();
services.AddOcppInboundHandler<TestHandler>(OcppProtocols.Ocpp201, "Heartbeat");
services.AddOcppInboundHandler<NestedCommandHandler>(OcppProtocols.Ocpp201, "NotifyEvent");
var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

var connections = provider.GetRequiredService<WebSocketManagerService>();
var pending = provider.GetRequiredService<OcppPendingRequestRegistry>();
var processor = provider.GetRequiredService<OCPPMessageProcessor>();
var registry = provider.GetRequiredService<OcppInboundHandlerRegistry>();

Check(registry.SupportedProtocols.SequenceEqual(new[] { OcppProtocols.Ocpp201 }), "only versions with handlers are supported");
var matcher = provider.GetRequiredService<WebSocketSubProtocolMatcher>();
Check(matcher.GetMatchingSubProtocol(new[] { "ocpp1.6" }) == null, "ocpp1.6 is not offered without 1.6 handlers");
Check(matcher.GetMatchingSubProtocol(new[] { "ocpp1.6", "ocpp2.0.1" }) == "ocpp2.0.1", "ocpp2.0.1 negotiated");
Check(new OcppInboundHandlerRegistry(new[]
{
    new OcppInboundHandlerRegistration(OcppProtocols.Ocpp16, "Heartbeat", typeof(TestHandler)),
    new OcppInboundHandlerRegistration(OcppProtocols.Ocpp201, "Heartbeat", typeof(TestHandler))
}).SupportedProtocols.SequenceEqual(new[] { OcppProtocols.Ocpp201, OcppProtocols.Ocpp16 }), "registering a 1.6 handler enables 1.6 (2.0.1 preferred)");

OcppConnection Connect(string id, FakeWebSocket socket, string protocol = OcppProtocols.Ocpp201)
{
    var connection = new OcppConnection(id, protocol, socket, new ChargePointStatus { Id = id, Protocol = protocol });
    connections.AddConnection(connection);
    return connection;
}

// Inbound CALLs through the processor
var ws1 = new FakeWebSocket();
var cp1 = Connect("CP1", ws1);
async Task<JsonElement[]> Inbound(string text)
{
    var before = ws1.Sent.Count;
    var queued = await processor.ProcessIncomingAsync(cp1, text, CancellationToken.None);
    if (queued != null) await processor.HandleCallAsync(cp1, queued, text, CancellationToken.None);
    return ws1.Sent.Count > before ? Elements(ws1.Sent.Last()) : Array.Empty<JsonElement>();
}

var reply = await Inbound("[2,\"m1\",\"Heartbeat\",{}]");
Check(reply.Length == 3 && reply[0].GetInt32() == 3 && reply[1].GetString() == "m1" && reply[2].GetProperty("currentTime").GetString() == "now", "handled CALL answered with [3,id,payload]");
reply = await Inbound("[2,\"m2\",\"Unknown\",{}]");
Check(reply.Length == 5 && reply[0].GetInt32() == 4 && reply[1].GetString() == "m2" && reply[2].GetString() == "NotImplemented", "unknown action -> NotImplemented");
reply = await Inbound("[2,\"m3\",\"Heartbeat\",{\"mode\":\"throw\"}]");
Check(reply[2].GetString() == "InternalError" && !reply[3].GetString()!.Contains("secret"), "handler exception -> InternalError without internal details");
reply = await Inbound("[2,\"m4\",\"Heartbeat\",{\"mode\":\"fail\"}]");
Check(reply[2].GetString() == "FormatViolation", "handler error code mapped to the 2.0.1 wire name");
reply = await Inbound("[2,\"m5\",\"Heartbeat\",\"oops\"]");
Check(reply.Length == 5 && reply[1].GetString() == "m5" && reply[2].GetString() == "FormatViolation", "malformed CALL payload -> CALLERROR FormatViolation");
reply = await Inbound("[2,\"m6\",\"Heartbeat\"]");
Check(reply.Length == 5 && reply[2].GetString() == "RpcFrameworkError", "malformed CALL frame -> CALLERROR RpcFrameworkError");
Check((await Inbound("garbage")).Length == 0, "frame without readable id is ignored");
Check((await Inbound("[3,\"zz\",\"bad\"]")).Length == 0, "malformed CALLRESULT is never answered");
Check((await Inbound("[3,\"unknown\",{}]")).Length == 0, "late CALLRESULT is ignored");
Check(TestHandler.Markers.Distinct().Count() == TestHandler.Markers.Count && TestHandler.Markers.Count >= 3, "every CALL gets a fresh DI scope");

// Outgoing commands: request/response
async Task<string> NextSentCall(FakeWebSocket socket, int countBefore)
{
    for (var i = 0; i < 200 && socket.Sent.Count <= countBefore; i++) await Task.Delay(10);
    return socket.Sent[countBefore];
}

var sentBefore = ws1.Sent.Count;
var resetTask = processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest { Type = ResetEnumType.OnIdle });
var sentCall = Elements(await NextSentCall(ws1, sentBefore));
Check(sentCall[0].GetInt32() == 2 && sentCall[2].GetString() == "Reset" && sentCall[3].GetProperty("type").GetString() == "OnIdle", "command sent as CALL with camelCase payload");
Check(pending.Count == 1, "request registered before the answer");
var otherSocket = new FakeWebSocket();
var cpOther = new OcppConnection("CP-OTHER", OcppProtocols.Ocpp201, otherSocket, new ChargePointStatus { Id = "CP-OTHER" });
await processor.ProcessIncomingAsync(cpOther, $"[3,\"{sentCall[1].GetString()}\",{{\"status\":\"Rejected\"}}]", CancellationToken.None);
Check(!resetTask.IsCompleted, "a reply from another connection does not complete the request");
await processor.ProcessIncomingAsync(cp1, $"[3,\"{sentCall[1].GetString()}\",{{\"status\":\"Scheduled\"}}]", CancellationToken.None);
var resetResponse = await resetTask;
Check(resetResponse.Status == ResetStatusEnumType.Scheduled && pending.Count == 0, "CALLRESULT completes the request with the typed response");

sentBefore = ws1.Sent.Count;
var unlockTask = processor.SendRequestAsync<UnlockConnectorRequest, UnlockConnectorResponse>("CP1", "UnlockConnector", new UnlockConnectorRequest { EvseId = 1, ConnectorId = 1 });
sentCall = Elements(await NextSentCall(ws1, sentBefore));
await processor.ProcessIncomingAsync(cp1, $"[4,\"{sentCall[1].GetString()}\",\"NotSupported\",\"no unlock\",{{}}]", CancellationToken.None);
await Throws<OcppCallErrorException>(() => unlockTask, "CALLERROR -> OcppCallErrorException with code and description",
    ex => ex.ErrorCode == "NotSupported" && ex.ErrorDescription == "no unlock");
Check(pending.Count == 0, "pending request cleaned up after CALLERROR");

await Throws<TimeoutException>(() => processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest(), TimeSpan.FromMilliseconds(150)),
    "no answer -> TimeoutException");
Check(pending.Count == 0, "pending request cleaned up after timeout");
await Throws<WebSocketNotFoundException>(() => processor.SendRequestAsync<ResetRequest, ResetResponse>("NOPE", "Reset", new ResetRequest()), "unknown charger -> WebSocketNotFoundException");

// One outstanding CALL per charger: the second command is sent only after the first is answered.
sentBefore = ws1.Sent.Count;
var first = processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest());
var firstCall = Elements(await NextSentCall(ws1, sentBefore));
var second = processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest());
await Task.Delay(100);
Check(ws1.Sent.Count == sentBefore + 1, "second CALL waits while the first is outstanding");
await processor.ProcessIncomingAsync(cp1, $"[3,\"{firstCall[1].GetString()}\",{{\"status\":\"Accepted\"}}]", CancellationToken.None);
var secondCall = Elements(await NextSentCall(ws1, sentBefore + 1));
await processor.ProcessIncomingAsync(cp1, $"[3,\"{secondCall[1].GetString()}\",{{\"status\":\"Rejected\"}}]", CancellationToken.None);
Check((await first).Status == ResetStatusEnumType.Accepted && (await second).Status == ResetStatusEnumType.Rejected, "queued commands each get their own answer");

// Send lock: concurrent senders never overlap on the socket.
var lockSocket = new FakeWebSocket { SendDelay = TimeSpan.FromMilliseconds(5) };
var lockConnection = new OcppConnection("CP-LOCK", OcppProtocols.Ocpp201, lockSocket, new ChargePointStatus { Id = "CP-LOCK" });
await Task.WhenAll(Enumerable.Range(0, 25).Select(i => Task.Run(() => lockConnection.SendTextAsync($"[3,\"{i}\",{{}}]", CancellationToken.None))));
Check(lockSocket.Sent.Count == 25 && lockSocket.MaxConcurrentSends == 1, "send lock serializes concurrent SendAsync calls");
var rawSocket = new FakeWebSocket { SendDelay = TimeSpan.FromMilliseconds(5) };
await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Task.Run(() => rawSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("x")), WebSocketMessageType.Text, true, CancellationToken.None))));
Check(rawSocket.MaxConcurrentSends > 1, "(fake socket detects overlapping sends without the lock)");

// ---------------------------------------------------------------- receive loop
var handler = provider.GetRequiredService<WebSocketHandler>();
var loopSocket = new FakeWebSocket();
var loopConnection = new OcppConnection("CP-LOOP", OcppProtocols.Ocpp201, loopSocket, new ChargePointStatus { Id = "CP-LOOP" });
var run = handler.RunConnectionAsync(loopConnection);
loopSocket.Enqueue("this is not json");
loopSocket.Enqueue("[2,\"b1\",\"Heartbeat\",{\"mode\":\"throw\"}]");
loopSocket.EnqueueFragments("[2,\"b2\",\"Heart", "beat\",{}]");
loopSocket.EnqueueBinary(new byte[] { 1, 2, 3 });
loopSocket.Enqueue("[2,\"b3\",\"Heartbeat\",{}]");
await loopSocket.WaitForSent(3);
Check(connections.GetConnection("CP-LOOP") == loopConnection, "connection registered while open");
var loopReplies = loopSocket.Sent.Select(Elements).ToList();
Check(loopReplies[0][1].GetString() == "b1" && loopReplies[0][2].GetString() == "InternalError", "loop: bad frame ignored, handler exception answered, socket kept open");
Check(loopReplies[1][0].GetInt32() == 3 && loopReplies[1][1].GetString() == "b2", "loop: fragmented frame reassembled");
Check(loopReplies[2][1].GetString() == "b3", "loop: binary frame ignored, next frame handled");

// A CALL handler that itself sends a command to the same charger must not deadlock the loop.
loopSocket.AutoReply = frame =>
{
    var e = Elements(frame);
    return e[0].GetInt32() == 2 && e[2].GetString() == "GetVariables" ? $"[3,\"{e[1].GetString()}\",{{\"getVariableResult\":[]}}]" : null;
};
loopSocket.Enqueue("[2,\"b4\",\"NotifyEvent\",{}]");
await loopSocket.WaitForSent(5);
var nestedReply = loopSocket.Sent.Select(Elements).Last();
Check(nestedReply[0].GetInt32() == 3 && nestedReply[1].GetString() == "b4" && nestedReply[2].GetProperty("nested").GetString() == "answered",
    "handler awaiting a command to the same charger completes (receive loop not blocked)");

loopSocket.Enqueue("[2,\"b5\",\"Heartbeat\",{\"pad\":\"" + new string('x', 9000) + "\"}]");
await run.WaitAsync(TimeSpan.FromSeconds(5));
Check(loopSocket.CloseStatus == WebSocketCloseStatus.MessageTooBig, "oversized message closes with MessageTooBig");
Check(connections.GetConnection("CP-LOOP") == null, "connection unregistered after close");

var closeSocket = new FakeWebSocket();
var closeConnection = new OcppConnection("CP-CLOSE", OcppProtocols.Ocpp201, closeSocket, new ChargePointStatus { Id = "CP-CLOSE" });
var closeRun = handler.RunConnectionAsync(closeConnection);
var waiting = processor.SendRequestAsync<ResetRequest, ResetResponse>("CP-CLOSE", "Reset", new ResetRequest());
await closeSocket.WaitForSent(1);
closeSocket.EnqueueClose();
await closeRun.WaitAsync(TimeSpan.FromSeconds(5));
Check(closeSocket.State == WebSocketState.Closed && closeSocket.CloseStatus == WebSocketCloseStatus.NormalClosure, "close frame answered with a normal close");
await Throws<WebSocketNotFoundException>(() => waiting, "pending command fails when the charger disconnects");
Check(connections.GetConnection("CP-CLOSE") == null && pending.Count == 0, "disconnect unregisters the socket and clears pending requests");

// Reconnect: the old connection is closed and its cleanup does not remove the new one.
var oldSocket = new FakeWebSocket();
var oldConnection = new OcppConnection("CP-RE", OcppProtocols.Ocpp201, oldSocket, new ChargePointStatus { Id = "CP-RE" });
var oldRun = handler.RunConnectionAsync(oldConnection);
var newSocket = new FakeWebSocket();
var newConnection = new OcppConnection("CP-RE", OcppProtocols.Ocpp201, newSocket, new ChargePointStatus { Id = "CP-RE" });
var newRun = handler.RunConnectionAsync(newConnection);
await oldRun.WaitAsync(TimeSpan.FromSeconds(5));
Check(connections.GetConnection("CP-RE") == newConnection, "reconnect supersedes the old connection");
newSocket.EnqueueClose();
await newRun.WaitAsync(TimeSpan.FromSeconds(5));

Console.WriteLine($"All {checks} OCPP checks passed.");

sealed class ScopeMarker
{
    public Guid Id { get; } = Guid.NewGuid();
}

sealed class TestHandler : IOCPPRequestHandler
{
    public static readonly ConcurrentBag<Guid> MarkersBag = new();
    public static List<Guid> Markers => MarkersBag.ToList();
    private readonly ScopeMarker _marker;

    public TestHandler(ScopeMarker marker) => _marker = marker;

    public Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
        MarkersBag.Add(_marker.Id);
        if (msgIn.JsonPayload!.Contains("throw")) throw new InvalidOperationException("secret database detail");
        if (msgIn.JsonPayload.Contains("fail")) return Task.FromResult(ErrorCodes.FormationViolation);
        msgOut.JsonPayload = "{\"currentTime\":\"now\"}";
        return Task.FromResult<string>(null!);
    }
}

sealed class NestedCommandHandler : IOCPPRequestHandler
{
    private readonly IOcppCommandSender _sender;

    public NestedCommandHandler(IOcppCommandSender sender) => _sender = sender;

    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
        await _sender.SendRequestAsync<GetVariablesRequest, GetVariablesResponse>(chargePointStatus.Id, "GetVariables", new GetVariablesRequest());
        msgOut.JsonPayload = "{\"nested\":\"answered\"}";
        return null!;
    }
}

sealed class FakeWebSocket : WebSocket
{
    private readonly Channel<(byte[] Data, WebSocketMessageType Type, bool End)> _incoming = Channel.CreateUnbounded<(byte[], WebSocketMessageType, bool)>();
    private WebSocketState _state = WebSocketState.Open;
    private WebSocketCloseStatus? _closeStatus;
    private int _concurrentSends;

    public List<string> Sent { get; } = new();
    public TimeSpan SendDelay { get; set; }
    public int MaxConcurrentSends { get; private set; }
    public Func<string, string?>? AutoReply { get; set; }

    public override WebSocketCloseStatus? CloseStatus => _closeStatus;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => _state;
    public override string? SubProtocol => "ocpp2.0.1";

    public void Enqueue(string text) => _incoming.Writer.TryWrite((Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true));
    public void EnqueueFragments(params string[] parts)
    {
        for (var i = 0; i < parts.Length; i++)
            _incoming.Writer.TryWrite((Encoding.UTF8.GetBytes(parts[i]), WebSocketMessageType.Text, i == parts.Length - 1));
    }
    public void EnqueueBinary(byte[] data) => _incoming.Writer.TryWrite((data, WebSocketMessageType.Binary, true));
    public void EnqueueClose() => _incoming.Writer.TryWrite((Array.Empty<byte>(), WebSocketMessageType.Close, true));

    public async Task WaitForSent(int count)
    {
        for (var i = 0; i < 500; i++)
        {
            lock (Sent) if (Sent.Count >= count) return;
            await Task.Delay(10);
        }
        throw new Exception($"FAILED: expected {count} sent frames, got {Sent.Count}");
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        var item = await _incoming.Reader.ReadAsync(cancellationToken);
        if (item.Type == WebSocketMessageType.Close)
        {
            _state = WebSocketState.CloseReceived;
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "bye");
        }
        // Larger payloads are delivered in buffer-sized chunks like a real socket.
        var count = Math.Min(buffer.Count, item.Data.Length);
        item.Data.AsSpan(0, count).CopyTo(buffer.AsSpan());
        if (count < item.Data.Length)
        {
            var rest = item.Data[count..];
            var queued = new List<(byte[], WebSocketMessageType, bool)> { (rest, item.Type, item.End) };
            while (_incoming.Reader.TryRead(out var next)) queued.Add(next);
            foreach (var q in queued) _incoming.Writer.TryWrite(q);
            return new WebSocketReceiveResult(count, item.Type, false);
        }
        return new WebSocketReceiveResult(count, item.Type, item.End);
    }

    public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        var now = Interlocked.Increment(ref _concurrentSends);
        lock (Sent) MaxConcurrentSends = Math.Max(MaxConcurrentSends, now);
        try
        {
            if (SendDelay > TimeSpan.Zero) await Task.Delay(SendDelay, cancellationToken);
            var text = Encoding.UTF8.GetString(buffer);
            lock (Sent) Sent.Add(text);
            var reply = AutoReply?.Invoke(text);
            if (reply != null) Enqueue(reply);
        }
        finally
        {
            Interlocked.Decrement(ref _concurrentSends);
        }
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        _closeStatus ??= closeStatus;
        _state = _state == WebSocketState.CloseReceived ? WebSocketState.Closed : WebSocketState.CloseSent;
        return Task.CompletedTask;
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        _closeStatus ??= closeStatus;
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override void Abort() => _state = WebSocketState.Aborted;
    public override void Dispose() { }
}

sealed class FakeHubContext : IHubContext<ChargerHub>
{
    public IHubClients Clients { get; } = new FakeHubClients();
    public IGroupManager Groups => throw new NotSupportedException();

    private sealed class FakeHubClients : IHubClients
    {
        private static readonly IClientProxy Proxy = new NullProxy();
        public IClientProxy All => Proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Client(string connectionId) => Proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
        public IClientProxy Group(string groupName) => Proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
        public IClientProxy User(string userId) => Proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class NullProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
