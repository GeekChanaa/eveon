using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPP.Core.Server;
using VoltaXApi.Authorization;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.ScaleOut;
using VoltaXApi.Services;

// Two API instances ("A" and "B") in one process, sharing an in-memory pub/sub bus and charger registry
// the way real instances share Redis.
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
async Task Eventually(Func<bool> condition, string name)
{
    for (var i = 0; i < 300 && !condition(); i++) await Task.Delay(10);
    Check(condition(), name);
}
JsonElement[] Elements(string frame) => JsonDocument.Parse(frame).RootElement.EnumerateArray().ToArray();

using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Critical));
var bus = new InMemoryScaleOutBus(loggerFactory.CreateLogger<InMemoryScaleOutBus>());
var registry = new InMemoryChargerConnectionRegistry(TimeSpan.FromSeconds(90));

Instance Build(string instanceId, IScaleOutBus? useBus = null, IChargerConnectionRegistry? useRegistry = null)
{
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ocpp:CommandTimeoutSeconds"] = "2" }).Build();
    var services = new ServiceCollection();
    services.AddLogging(b => b.SetMinimumLevel(LogLevel.Critical));
    services.AddSingleton<IConfiguration>(config);
    services.AddSingleton(new ScaleOutSettings(instanceId, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(90)));
    services.AddSingleton<IScaleOutBus>(useBus ?? bus);
    services.AddSingleton<IChargerConnectionRegistry>(useRegistry ?? registry);
    services.AddSingleton<WebSocketManagerService>();
    services.AddSingleton<OcppPendingRequestRegistry>();
    services.AddSingleton<OcppInboundHandlerRegistry>();
    services.AddSingleton<OCPPRequestHandler>();
    services.AddSingleton<RemoteOcppCommandClient>();
    services.AddSingleton<ChargerOwnershipTracker>();
    services.AddSingleton<OCPPMessageProcessor>();
    services.AddSingleton<IOcppCommandSender>(sp => sp.GetRequiredService<OCPPMessageProcessor>());
    services.AddSingleton<ChargePointStatusManagerService>();
    services.AddSingleton<IHubContext<ChargerHub>, FakeHubContext>();
    services.AddSingleton<ChargePointConnectivityNotifier>();
    services.AddSingleton<WebSocketHandler>();
    services.AddSingleton<HubConnections>();
    services.AddSingleton<ScaleOutCoordinator>();
    var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    return new Instance(instanceId, provider);
}

var a = Build("A");
var b = Build("B");
await a.Coordinator.StartAsync(CancellationToken.None);
await b.Coordinator.StartAsync(CancellationToken.None);
await a.Coordinator.Subscribed.WaitAsync(TimeSpan.FromSeconds(5));
await b.Coordinator.Subscribed.WaitAsync(TimeSpan.FromSeconds(5));

// ---------------------------------------------------------------- ownership + status view
var socketA = new FakeWebSocket();
var connA = new OcppConnection("CP1", OcppProtocols.Ocpp201, socketA, new ChargePointStatus { Id = "CP1", Protocol = OcppProtocols.Ocpp201 });
var runA = a.Handler.RunConnectionAsync(connA);
await Eventually(() => registry.Get("CP1")?.InstanceId == "A", "connect registers the owner instance");
Check(registry.Get("CP1")!.ProtocolVersion == OcppProtocols.Ocpp201, "registry stores the protocol version");
Check(b.Processor.GetProtocolVersion("CP1") == OcppProtocols.Ocpp201, "remote instance reads the protocol version from the registry");
Check(b.Status.ChargePointExists("CP1") && b.Status.GetChargePointStatus("CP1")?.Protocol == OcppProtocols.Ocpp201, "remote instance sees the charger as connected");
Check(!b.Status.ChargePointExists("NOPE") && b.Processor.GetProtocolVersion("NOPE") == null, "unknown charger is offline everywhere");
Check(a.Status.GetChargePointStatus("CP1") == connA.Status, "owner instance returns its live status");

// ---------------------------------------------------------------- command routing
socketA.AutoReply = frame =>
{
    var e = Elements(frame);
    if (e[0].GetInt32() != 2) return null;
    var id = e[1].GetString();
    return e[2].GetString() switch
    {
        "Reset" => $"[3,\"{id}\",{{\"status\":\"Scheduled\"}}]",
        "UnlockConnector" => $"[4,\"{id}\",\"NotSupported\",\"no unlock\",{{\"x\":1}}]",
        _ => null
    };
};
var reset = await b.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest { Type = ResetEnumType.OnIdle });
Check(reset.Status == ResetStatusEnumType.Scheduled, "ok: command from B executed on A's socket, typed response returned");
var resetFrame = socketA.Sent.Select(Elements).First(e => e[0].GetInt32() == 2 && e[2].GetString() == "Reset");
Check(resetFrame[3].GetProperty("type").GetString() == "OnIdle", "forwarded payload reaches the charger unchanged");

await Throws<OcppCallErrorException>(() => b.Processor.SendRequestAsync<UnlockConnectorRequest, UnlockConnectorResponse>("CP1", "UnlockConnector",
        new UnlockConnectorRequest { EvseId = 1, ConnectorId = 1 }),
    "callerror: mapped to OcppCallErrorException with code, description and details",
    ex => ex.ErrorCode == "NotSupported" && ex.ErrorDescription == "no unlock" && ex.ErrorDetails == "{\"x\":1}");

var watch = Stopwatch.StartNew();
await Throws<TimeoutException>(() => b.Processor.SendRequestAsync<GetVariablesRequest, GetVariablesResponse>("CP1", "GetVariables",
        new GetVariablesRequest(), TimeSpan.FromMilliseconds(300)),
    "timeout: charger silent -> TimeoutException on the calling instance");
Check(watch.Elapsed < TimeSpan.FromSeconds(2), "timeout honoured end to end (owner enforces it, caller gets the answer)");
Check(b.Remote.PendingCount == 0, "caller cleans up its pending remote request");

await registry.ClaimAsync("GHOST", new ChargerOwnership("A", OcppProtocols.Ocpp201, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
await Throws<WebSocketNotFoundException>(() => b.Processor.SendRequestAsync<ResetRequest, ResetResponse>("GHOST", "Reset", new ResetRequest()),
    "notconnected: owner has no socket -> WebSocketNotFoundException", ex => ex is not ScaleOutUnavailableException);
await registry.ClaimAsync("ORPHAN", new ChargerOwnership("C", OcppProtocols.Ocpp201, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
await Throws<WebSocketNotFoundException>(() => b.Processor.SendRequestAsync<ResetRequest, ResetResponse>("ORPHAN", "Reset", new ResetRequest()),
    "owner instance gone (nobody subscribed) -> fails fast as not connected");
await Throws<WebSocketNotFoundException>(() => b.Processor.SendRequestAsync<ResetRequest, ResetResponse>("NOPE", "Reset", new ResetRequest()),
    "charger owned by nobody -> WebSocketNotFoundException");

var failingClient = new RemoteOcppCommandClient(new FailingBus(), registry, new ScaleOutSettings("D", null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(90)),
    loggerFactory.CreateLogger<RemoteOcppCommandClient>());
await Throws<ScaleOutUnavailableException>(() => failingClient.SendAsync("CP1", "Reset", "{}", TimeSpan.FromSeconds(1), CancellationToken.None),
    "bus down -> ScaleOutUnavailableException (fails fast)");

Check(RemoteOcppCommandClient.ToReply("r", "{}", null).Status == OcppCommandReply.Ok
      && RemoteOcppCommandClient.ToReply("r", null, new OcppCallErrorException("X", "d", "{}")).Status == OcppCommandReply.CallError
      && RemoteOcppCommandClient.ToReply("r", null, new TimeoutException("t")).Status == OcppCommandReply.Timeout
      && RemoteOcppCommandClient.ToReply("r", null, new WebSocketNotFoundException("n")).Status == OcppCommandReply.NotConnected,
    "owner maps local outcomes to ok/callerror/timeout/notconnected");

// A local command on the owner still takes the local path.
var localReset = await a.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest());
Check(localReset.Status == ResetStatusEnumType.Scheduled && a.Remote.PendingCount == 0, "owner sends to its own charger directly");

// ---------------------------------------------------------------- stale-owner takeover
var socketB = new FakeWebSocket { AutoReply = socketA.AutoReply };
var connB = new OcppConnection("CP1", OcppProtocols.Ocpp201, socketB, new ChargePointStatus { Id = "CP1", Protocol = OcppProtocols.Ocpp201 });
var runB = b.Handler.RunConnectionAsync(connB);
await runA.WaitAsync(TimeSpan.FromSeconds(5));
Check(connA.Closed.IsCancellationRequested, "close-stale: the old instance closed its socket when the charger reconnected elsewhere");
Check(registry.Get("CP1")?.InstanceId == "B", "the old instance's cleanup does not release the new owner's entry");
Check(a.Connections.GetConnection("CP1") == null && b.Connections.GetConnection("CP1") == connB, "the charger is live on B only");
var sentOnB = socketB.Sent.Count;
reset = await a.Processor.SendRequestAsync<ResetRequest, ResetResponse>("CP1", "Reset", new ResetRequest());
Check(reset.Status == ResetStatusEnumType.Scheduled && socketB.Sent.Count > sentOnB, "after takeover, commands from A are routed to B");

// Heartbeat: a local socket whose ownership was taken over (missed close-stale) is closed.
await registry.ClaimAsync("CP1", new ChargerOwnership("A", OcppProtocols.Ocpp201, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
await b.Ownership.HeartbeatAsync();
await runB.WaitAsync(TimeSpan.FromSeconds(5));
Check(connB.Closed.IsCancellationRequested && registry.Get("CP1")?.InstanceId == "A", "heartbeat closes a socket that lost its ownership and keeps the new owner");
await registry.ReleaseAsync("CP1", "A");

// Clean disconnect releases the entry.
var socketC = new FakeWebSocket();
var runC = a.Handler.RunConnectionAsync(new OcppConnection("CP2", OcppProtocols.Ocpp201, socketC, new ChargePointStatus { Id = "CP2" }));
await Eventually(() => registry.Get("CP2")?.InstanceId == "A", "second charger registered");
socketC.EnqueueClose();
await runC.WaitAsync(TimeSpan.FromSeconds(5));
Check(registry.Get("CP2") == null, "disconnect removes the entry owned by this instance");

// Registry primitives
var shortTtl = new InMemoryChargerConnectionRegistry(TimeSpan.FromMilliseconds(50));
await shortTtl.ClaimAsync("X", new ChargerOwnership("A", "p", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
await Task.Delay(100);
Check(shortTtl.Get("X") == null, "entry expires when the owner stops refreshing");
Check(await shortTtl.RefreshAsync("X", new ChargerOwnership("B", "p", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)) && shortTtl.Get("X")?.InstanceId == "B",
    "an expired entry can be taken by another instance");
Check(!await shortTtl.ReleaseAsync("X", "A") && shortTtl.Get("X") != null, "release by a non-owner is refused");

// ---------------------------------------------------------------- SignalR revocation broadcast
var userOnA = new FakeHubCallerContext("a-1");
var userOnB = new FakeHubCallerContext("b-1");
var otherOnB = new FakeHubCallerContext("b-2");
a.HubConnections.Add(7, userOnA);
b.HubConnections.Add(7, userOnB);
b.HubConnections.Add(8, otherOnB);
a.HubConnections.Revoke(new[] { 7 });
Check(userOnA.Aborted, "revocation aborts the local connection immediately");
await Eventually(() => userOnB.Aborted, "revocation is broadcast: the user's connection on the other instance is aborted");
Check(!otherOnB.Aborted, "other users' connections are untouched");
var standalone = new HubConnections();
var alone = new FakeHubCallerContext("s-1");
standalone.Add(9, alone);
standalone.Revoke(new[] { 9 });
Check(alone.Aborted, "without a bus revocation stays local");

// ---------------------------------------------------------------- security state (in-memory mode)
var store = new DistributedCacheSecurityStateStore(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
await store.SetAsync("ticket", "42", TimeSpan.FromMinutes(1));
var takes = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => store.TakeAsync("ticket"))));
Check(takes.Count(t => t == "42") == 1 && takes.Count(t => t == null) == 19, "one-time ticket is taken exactly once under concurrency");
var counts = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => store.IncrementAsync("attempts", TimeSpan.FromMinutes(1)))));
Check(counts.OrderBy(c => c).SequenceEqual(Enumerable.Range(1, 10).Select(i => (long)i)), "attempt counter increments atomically");
var adds = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => store.TryAddAsync("used", "1", TimeSpan.FromMinutes(1)))));
Check(adds.Count(x => x) == 1 && await store.ExistsAsync("used"), "used-marker is claimed once");
await store.SetAsync("short", "v", TimeSpan.FromMilliseconds(50));
await Task.Delay(120);
Check(await store.TakeAsync("short") == null, "TTL is kept");

await a.Coordinator.StopAsync(CancellationToken.None);
await b.Coordinator.StopAsync(CancellationToken.None);

// ---------------------------------------------------------------- Redis down (unreachable endpoint)
await using (var downRedis = new RedisConnection("127.0.0.1:1,connectTimeout=500,connectRetry=1"))
{
    var downSettings = new ScaleOutSettings("DOWN", "127.0.0.1:1", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(90));
    var downRegistry = new RedisChargerConnectionRegistry(downRedis, downSettings);
    var downBus = new RedisScaleOutBus(downRedis, loggerFactory.CreateLogger<RedisScaleOutBus>());
    var downClient = new RemoteOcppCommandClient(downBus, downRegistry, downSettings, loggerFactory.CreateLogger<RemoteOcppCommandClient>());
    await downRedis.GetAsync();
    var downWatch = Stopwatch.StartNew();
    await Throws<ScaleOutUnavailableException>(() => downClient.SendAsync("CP1", "Reset", "{}", TimeSpan.FromSeconds(10), CancellationToken.None),
        "redis down: remote command fails with ScaleOutUnavailableException");
    Check(downWatch.Elapsed < TimeSpan.FromSeconds(2), "redis down: it fails fast, not after the command timeout");
    var downTracker = new ChargerOwnershipTracker(downRegistry, downBus, new WebSocketManagerService(), downSettings, loggerFactory.CreateLogger<ChargerOwnershipTracker>());
    var downConn = new OcppConnection("CP-DOWN", OcppProtocols.Ocpp201, new FakeWebSocket(), new ChargePointStatus { Id = "CP-DOWN" });
    await downTracker.OnConnectedAsync(downConn);
    Check(await downTracker.OnDisconnectedAsync(downConn), "redis down: local chargers still connect, and disconnect as offline");
    Check(downClient.FindRemoteOwner("CP1") == null, "redis down: status view falls back to local only");
}

// ---------------------------------------------------------------- Redis implementations (optional: SCALEOUT_REDIS=host:port)
var redisAddress = Environment.GetEnvironmentVariable("SCALEOUT_REDIS");
if (string.IsNullOrWhiteSpace(redisAddress))
{
    Console.WriteLine("SKIP: Redis checks (set SCALEOUT_REDIS=host:port to run them)");
}
else
{
    await using var redis = new RedisConnection(redisAddress);
    var redisSettings = new ScaleOutSettings("R", redisAddress, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(90));
    var redisRegistry = new RedisChargerConnectionRegistry(redis, redisSettings);
    var redisBus = new RedisScaleOutBus(redis, loggerFactory.CreateLogger<RedisScaleOutBus>());
    var id = "CHK-" + Guid.NewGuid().ToString("N")[..8];
    var now = DateTimeOffset.UtcNow;

    Check(await redisRegistry.ClaimAsync(id, new ChargerOwnership("RA", "ocpp2.0.1", now, now)) == null, "redis: first claim has no previous owner");
    Check((await redisRegistry.ClaimAsync(id, new ChargerOwnership("RB", "ocpp2.0.1", now, now)))?.InstanceId == "RA", "redis: takeover returns the previous owner");
    Check(!await redisRegistry.RefreshAsync(id, new ChargerOwnership("RA", "ocpp2.0.1", now, now)), "redis: stale owner cannot refresh");
    Check(await redisRegistry.RefreshAsync(id, new ChargerOwnership("RB", "ocpp2.0.1", now, now)), "redis: owner refreshes");
    Check(!await redisRegistry.ReleaseAsync(id, "RA") && redisRegistry.Get(id)?.InstanceId == "RB", "redis: non-owner release refused");
    var ttl = await (await redis.GetAsync()).GetDatabase().KeyTimeToLiveAsync($"eveon:ocpp:owner:{id}");
    Check(ttl > TimeSpan.FromSeconds(80) && ttl <= TimeSpan.FromSeconds(90), "redis: ownership key carries the TTL");
    Check(await redisRegistry.ReleaseAsync(id, "RB") && await redisRegistry.GetAsync(id) == null, "redis: owner release deletes the key");

    var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    await redisBus.SubscribeAsync("eveon:check:" + id, m => { received.TrySetResult(m); return Task.CompletedTask; });
    Check(await redisBus.PublishAsync("eveon:check:" + id, "hello") == 1 && await received.Task.WaitAsync(TimeSpan.FromSeconds(5)) == "hello", "redis: pub/sub round trip");
    Check(await redisBus.PublishAsync("eveon:check:nobody:" + id, "x") == 0, "redis: publish reports 0 receivers when nobody listens");

    var redisStore = new DistributedCacheSecurityStateStore(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), redis);
    await redisStore.SetAsync("t:" + id, "42", TimeSpan.FromMinutes(1));
    var redisTakes = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => redisStore.TakeAsync("t:" + id)));
    Check(redisTakes.Count(t => t == "42") == 1, "redis: GETDEL spends a ticket once");
    var redisCounts = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => redisStore.IncrementAsync("c:" + id, TimeSpan.FromMinutes(1))));
    Check(redisCounts.OrderBy(c => c).SequenceEqual(Enumerable.Range(1, 10).Select(i => (long)i)), "redis: INCR counts atomically");
    var redisAdds = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => redisStore.TryAddAsync("u:" + id, "1", TimeSpan.FromMinutes(1))));
    Check(redisAdds.Count(x => x) == 1 && await redisStore.ExistsAsync("u:" + id), "redis: SET NX claims once");

    var ra = Build("RA-" + id, redisBus, redisRegistry);
    var rb = Build("RB-" + id, redisBus, redisRegistry);
    await ra.Coordinator.StartAsync(CancellationToken.None);
    await rb.Coordinator.StartAsync(CancellationToken.None);
    await ra.Coordinator.Subscribed.WaitAsync(TimeSpan.FromSeconds(5));
    await rb.Coordinator.Subscribed.WaitAsync(TimeSpan.FromSeconds(5));
    var redisSocket = new FakeWebSocket { AutoReply = socketA.AutoReply };
    var redisConn = new OcppConnection(id, OcppProtocols.Ocpp201, redisSocket, new ChargePointStatus { Id = id, Protocol = OcppProtocols.Ocpp201 });
    var redisRun = ra.Handler.RunConnectionAsync(redisConn);
    await Eventually(() => redisRegistry.Get(id)?.InstanceId == ra.Id, "redis: connect registers the owner");
    var redisReset = await rb.Processor.SendRequestAsync<ResetRequest, ResetResponse>(id, "Reset", new ResetRequest());
    Check(redisReset.Status == ResetStatusEnumType.Scheduled, "redis: command routed over Redis to the owning instance");
    await Throws<OcppCallErrorException>(() => rb.Processor.SendRequestAsync<UnlockConnectorRequest, UnlockConnectorResponse>(id, "UnlockConnector",
        new UnlockConnectorRequest { EvseId = 1, ConnectorId = 1 }), "redis: CALLERROR mapped across instances", ex => ex.ErrorCode == "NotSupported");
    var rbSocket = new FakeWebSocket();
    var rbRun = rb.Handler.RunConnectionAsync(new OcppConnection(id, OcppProtocols.Ocpp201, rbSocket, new ChargePointStatus { Id = id }));
    await redisRun.WaitAsync(TimeSpan.FromSeconds(5));
    Check(redisRegistry.Get(id)?.InstanceId == rb.Id, "redis: stale owner closed on takeover, new owner kept");
    rbSocket.EnqueueClose();
    await rbRun.WaitAsync(TimeSpan.FromSeconds(5));
    Check(await redisRegistry.GetAsync(id) == null, "redis: disconnect releases the key");
    await ra.Coordinator.StopAsync(CancellationToken.None);
    await rb.Coordinator.StopAsync(CancellationToken.None);
}

Console.WriteLine($"All {checks} scale-out checks passed.");

sealed class Instance
{
    public Instance(string id, ServiceProvider provider)
    {
        Id = id;
        Processor = provider.GetRequiredService<OCPPMessageProcessor>();
        Handler = provider.GetRequiredService<WebSocketHandler>();
        Connections = provider.GetRequiredService<WebSocketManagerService>();
        Status = provider.GetRequiredService<ChargePointStatusManagerService>();
        Remote = provider.GetRequiredService<RemoteOcppCommandClient>();
        Ownership = provider.GetRequiredService<ChargerOwnershipTracker>();
        HubConnections = provider.GetRequiredService<HubConnections>();
        Coordinator = provider.GetRequiredService<ScaleOutCoordinator>();
    }

    public string Id { get; }
    public OCPPMessageProcessor Processor { get; }
    public WebSocketHandler Handler { get; }
    public WebSocketManagerService Connections { get; }
    public ChargePointStatusManagerService Status { get; }
    public RemoteOcppCommandClient Remote { get; }
    public ChargerOwnershipTracker Ownership { get; }
    public HubConnections HubConnections { get; }
    public ScaleOutCoordinator Coordinator { get; }
}

sealed class FailingBus : IScaleOutBus
{
    public Task<long> PublishAsync(string channel, string message) => throw new InvalidOperationException("Redis is down");
    public Task SubscribeAsync(string channel, Func<string, Task> handler) => throw new InvalidOperationException("Redis is down");
}

sealed class FakeHubCallerContext : HubCallerContext
{
    public FakeHubCallerContext(string connectionId) => ConnectionId = connectionId;
    public bool Aborted { get; private set; }
    public override string ConnectionId { get; }
    public override string? UserIdentifier => null;
    public override ClaimsPrincipal? User => null;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;
    public override void Abort() => Aborted = true;
}

sealed class FakeWebSocket : WebSocket
{
    private readonly Channel<(byte[] Data, WebSocketMessageType Type)> _incoming = Channel.CreateUnbounded<(byte[], WebSocketMessageType)>();
    private WebSocketState _state = WebSocketState.Open;
    private WebSocketCloseStatus? _closeStatus;

    public List<string> Sent { get; } = new();
    public Func<string, string?>? AutoReply { get; set; }

    public override WebSocketCloseStatus? CloseStatus => _closeStatus;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => _state;
    public override string? SubProtocol => "ocpp2.0.1";

    public void Enqueue(string text) => _incoming.Writer.TryWrite((Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text));
    public void EnqueueClose() => _incoming.Writer.TryWrite((Array.Empty<byte>(), WebSocketMessageType.Close));

    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        var item = await _incoming.Reader.ReadAsync(cancellationToken);
        if (item.Type == WebSocketMessageType.Close)
        {
            _state = WebSocketState.CloseReceived;
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "bye");
        }
        item.Data.CopyTo(buffer.AsSpan());
        return new WebSocketReceiveResult(item.Data.Length, item.Type, true);
    }

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(buffer);
        lock (Sent) Sent.Add(text);
        var reply = AutoReply?.Invoke(text);
        if (reply != null) Enqueue(reply);
        return Task.CompletedTask;
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
