using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace VoltaX.Tests.Infrastructure;

/// <summary>
/// A hub context that records what the server pushes instead of sending it: who it was addressed to (the groups,
/// users or connections named) and the message. Register it in place of IHubContext&lt;THub&gt;.
/// </summary>
public sealed class RecordingHubContext<THub> : IHubContext<THub> where THub : Hub
{
    private readonly List<Push> _sent = new();

    public sealed record Push(string Target, IReadOnlyList<string> Recipients, string Method, object?[] Args);

    public RecordingHubContext()
    {
        var clients = DispatchProxy.Create<IHubClients, RecordingClients>();
        ((RecordingClients)(object)clients).Owner = this;
        Clients = clients;
    }

    public IHubClients Clients { get; }
    public IGroupManager Groups { get; } = Stub<IGroupManager>.Create();

    public IReadOnlyList<Push> Sent { get { lock (_sent) return _sent.ToList(); } }

    /// <summary>The pushes of <paramref name="method"/> that reached <paramref name="group"/>.</summary>
    public IEnumerable<object?[]> ToGroup(string group, string method) =>
        Sent.Where(p => p.Method == method && p.Target is "Group" or "Groups" && p.Recipients.Contains(group)).Select(p => p.Args);

    /// <summary>The first argument of each push of <paramref name="method"/> to <paramref name="group"/>.</summary>
    public IEnumerable<object?> SentTo(string group, string method) =>
        ToGroup(group, method).Select(args => args.Length > 0 ? args[0] : null);

    internal void Record(Push push)
    {
        lock (_sent) _sent.Add(push);
    }

    /// <summary>Any IHubClients member (Group, Groups, User, All...) returns a proxy that records the send.</summary>
    public class RecordingClients : DispatchProxy
    {
        internal RecordingHubContext<THub> Owner = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            var target = method!.Name.StartsWith("get_", StringComparison.Ordinal) ? method.Name[4..] : method.Name;
            var recipients = (args ?? Array.Empty<object?>()).SelectMany(a => a switch
            {
                string single => new[] { single },
                IEnumerable<string> many => many,
                _ => Array.Empty<string>()
            }).ToList();
            return new RecordingProxy(Owner, target, recipients);
        }
    }

    private sealed class RecordingProxy(RecordingHubContext<THub> owner, string target, IReadOnlyList<string> recipients) : ISingleClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            owner.Record(new Push(target, recipients, method, args));
            return Task.CompletedTask;
        }

        public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

/// <summary>A hub connection for calling hub methods directly in tests.</summary>
public sealed class FakeHubCallerContext(ClaimsPrincipal? user, string connectionId = "connection-1") : HubCallerContext
{
    public override string ConnectionId => connectionId;
    public override string? UserIdentifier => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public override ClaimsPrincipal? User => user;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;
    public override void Abort() { }

    /// <summary>An authenticated principal for <paramref name="userId"/>, as the JWT handler builds it.</summary>
    public static ClaimsPrincipal SignedIn(int userId) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Bearer"));
}
