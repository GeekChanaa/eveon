using System.Net.WebSockets;
using System.Reflection;
using Newtonsoft.Json;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Core;
using VoltaXApi.Services;

namespace VoltaX.Tests.Infrastructure;

/// <summary>Records every command the CSMS sends to a charger and answers with <see cref="Reply"/>.</summary>
public sealed class FakeCommandSender : IOcppCommandSender
{
    private readonly object _lock = new();
    public Dictionary<string, string> Versions { get; } = new();
    public Func<string, object> Reply { get; set; } = _ => new { status = "Accepted" };
    public List<(string ChargePointId, string Action, string Payload)> Sent { get; } = new();
    public int Count { get { lock (_lock) return Sent.Count; } }
    public string? LastAction { get { lock (_lock) return Sent.Count == 0 ? null : Sent[^1].Action; } }
    public string? LastPayload { get { lock (_lock) return Sent.Count == 0 ? null : Sent[^1].Payload; } }

    public string? GetProtocolVersion(string chargePointId) => Versions.TryGetValue(chargePointId, out var v) ? v : null;

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(string chargePointId, string action, TRequest request,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var payload = JsonConvert.SerializeObject(request, OCPPMessageFactory.DefaultSettings);
        lock (_lock) Sent.Add((chargePointId, action, payload));
        var reply = JsonConvert.SerializeObject(Reply(action), OCPPMessageFactory.DefaultSettings);
        return Task.FromResult(JsonConvert.DeserializeObject<TResponse>(reply, OCPPMessageFactory.DefaultSettings)!);
    }

    /// <summary>Waits for a background command (low-balance stop, CostUpdated) to reach the sender.</summary>
    public async Task<bool> WaitFor(string action, TimeSpan? timeout = null)
    {
        var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < until)
        {
            lock (_lock)
                if (Sent.Any(s => s.Action == action))
                    return true;
            await Task.Delay(20);
        }
        return false;
    }
}

public sealed class NoExternalTokens : IExternalTokenAuthorizer
{
    public Task<AuthorizationStatusEnumType?> AuthorizeAsync(string? idToken, string? chargePointId, CancellationToken cancellationToken = default) =>
        Task.FromResult<AuthorizationStatusEnumType?>(null);
}

public sealed class NoRoaming : IRoamingTransactionObserver
{
    public Task<AuthorizationStatusEnumType?> OnTransactionEventAsync(string chargePointId, TransactionEventRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<AuthorizationStatusEnumType?>(null);
}

public sealed class NoLoadBalancing : VoltaXApi.SmartCharging.ILoadBalancingTrigger
{
    public void RequestRebalance(int chargingStationId) { }
    public void RequestRebalanceForChargePoint(string chargePointId) { }
    public Task<VoltaXApi.SmartCharging.RebalanceSummary> RebalanceNowAsync(int chargingStationId, string reason, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>Interface stub: every method returns default / a completed task.</summary>
public class Stub<T> : DispatchProxy where T : class
{
    public static T Create() => DispatchProxy.Create<T, Stub<T>>();

    protected override object? Invoke(MethodInfo? method, object?[]? args) => DefaultResult(method!.ReturnType);

    internal static object? DefaultResult(Type type)
    {
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

/// <summary>Interface stub that records each call (method name and arguments).</summary>
public class Recorder<T> : DispatchProxy where T : class
{
    private readonly List<(string Method, object?[] Args)> _calls = new();

    public IReadOnlyList<(string Method, object?[] Args)> Calls { get { lock (_calls) return _calls.ToList(); } }

    public IEnumerable<object?[]> CallsTo(string method) => Calls.Where(c => c.Method == method).Select(c => c.Args);

    public static (T Proxy, Recorder<T> Recorder) Create()
    {
        var proxy = DispatchProxy.Create<T, Recorder<T>>();
        return (proxy, (Recorder<T>)(object)proxy);
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        lock (_calls) _calls.Add((method!.Name, args ?? Array.Empty<object?>()));
        return Stub<T>.DefaultResult(method.ReturnType);
    }
}

public sealed class IdleWebSocket(string subProtocol) : WebSocket
{
    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => WebSocketState.Open;
    public override string? SubProtocol => subProtocol;
    public override void Abort() { }
    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
    public override void Dispose() { }
    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();
    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) => Task.CompletedTask;
}
