using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;

namespace VoltaX.Tests.Infrastructure;

/// <summary>
/// A charger's end of a WebSocket, scripted by the test: frames queued with <see cref="Enqueue"/> are received by the
/// CSMS (split into buffer-sized chunks like a real socket), frames the CSMS sends are recorded in <see cref="Sent"/>
/// and can be answered automatically with <see cref="AutoReply"/>.
/// </summary>
public sealed class ScriptedWebSocket(string subProtocol = "ocpp2.0.1") : WebSocket
{
    private readonly Channel<(byte[] Data, WebSocketMessageType Type, bool End)> _incoming = Channel.CreateUnbounded<(byte[], WebSocketMessageType, bool)>();
    private readonly List<string> _sent = new();
    private readonly List<(int Count, TaskCompletionSource Done)> _waiters = new();
    private (byte[] Data, WebSocketMessageType Type, bool End)? _partial;
    private WebSocketState _state = WebSocketState.Open;
    private WebSocketCloseStatus? _closeStatus;
    private int _concurrentSends;
    private int _maxConcurrentSends;

    /// <summary>Every frame the CSMS sent, in order.</summary>
    public IReadOnlyList<string> Sent { get { lock (_sent) return _sent.ToList(); } }

    /// <summary>The highest number of SendAsync calls that ran at the same time.</summary>
    public int MaxConcurrentSends => Volatile.Read(ref _maxConcurrentSends);

    /// <summary>Makes each send take this long, so overlapping sends can be detected.</summary>
    public TimeSpan SendDelay { get; set; }

    /// <summary>Answer to a sent frame (queued as if the charger sent it), or null for none.</summary>
    public Func<string, string?>? AutoReply { get; set; }

    public override WebSocketCloseStatus? CloseStatus => _closeStatus;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => _state;
    public override string? SubProtocol => subProtocol;

    public static JsonElement[] Elements(string frame) => JsonDocument.Parse(frame).RootElement.EnumerateArray().ToArray();

    public void Enqueue(string text) => _incoming.Writer.TryWrite((Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true));

    public void EnqueueFragments(params string[] parts)
    {
        for (var i = 0; i < parts.Length; i++)
            _incoming.Writer.TryWrite((Encoding.UTF8.GetBytes(parts[i]), WebSocketMessageType.Text, i == parts.Length - 1));
    }

    public void EnqueueBinary(byte[] data) => _incoming.Writer.TryWrite((data, WebSocketMessageType.Binary, true));

    public void EnqueueClose() => _incoming.Writer.TryWrite((Array.Empty<byte>(), WebSocketMessageType.Close, true));

    /// <summary>Completes once at least <paramref name="count"/> frames were sent (fails the test after 5 s).</summary>
    public async Task WaitForSent(int count)
    {
        TaskCompletionSource done;
        lock (_sent)
        {
            if (_sent.Count >= count)
                return;
            done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, done));
        }
        try
        {
            await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            throw new Xunit.Sdk.XunitException($"expected {count} sent frames, got {Sent.Count}");
        }
    }

    /// <summary>The frame at <paramref name="index"/>, once it was sent.</summary>
    public async Task<JsonElement[]> SentFrame(int index)
    {
        await WaitForSent(index + 1);
        return Elements(Sent[index]);
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        var item = _partial ?? await _incoming.Reader.ReadAsync(cancellationToken);
        _partial = null;
        if (item.Type == WebSocketMessageType.Close)
        {
            _state = WebSocketState.CloseReceived;
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "bye");
        }
        var count = Math.Min(buffer.Count, item.Data.Length);
        item.Data.AsSpan(0, count).CopyTo(buffer.AsSpan());
        if (count < item.Data.Length)
        {
            _partial = (item.Data[count..], item.Type, item.End);
            return new WebSocketReceiveResult(count, item.Type, false);
        }
        return new WebSocketReceiveResult(count, item.Type, item.End);
    }

    public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        var now = Interlocked.Increment(ref _concurrentSends);
        int max;
        while (now > (max = Volatile.Read(ref _maxConcurrentSends)) && Interlocked.CompareExchange(ref _maxConcurrentSends, now, max) != max) { }
        try
        {
            if (SendDelay > TimeSpan.Zero)
                await Task.Delay(SendDelay, cancellationToken);
            var text = Encoding.UTF8.GetString(buffer);
            List<TaskCompletionSource> ready;
            lock (_sent)
            {
                _sent.Add(text);
                ready = _waiters.Where(w => w.Count <= _sent.Count).Select(w => w.Done).ToList();
                _waiters.RemoveAll(w => w.Count <= _sent.Count);
            }
            ready.ForEach(r => r.TrySetResult());
            var reply = AutoReply?.Invoke(text);
            if (reply != null)
                Enqueue(reply);
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

/// <summary>A SignalR hub context whose messages go nowhere (the dashboard mirror of charger frames).</summary>
public sealed class NullHubContext<THub> : IHubContext<THub> where THub : Hub
{
    public IHubClients Clients { get; } = new NullClients();
    public IGroupManager Groups => throw new NotSupportedException();

    private sealed class NullClients : IHubClients
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
