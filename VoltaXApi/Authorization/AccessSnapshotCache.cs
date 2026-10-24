using System.Collections.Concurrent;
using VoltaXApi.Data;

namespace VoltaXApi.Authorization;

/// <summary>
/// Short-lived cache of <see cref="AccessSnapshot"/> per user, so a request does not reload the user, role and
/// permissions every time. Entries live <see cref="Lifetime"/> (configuration "Authorization:AccessCacheSeconds",
/// default 10; 0 turns the cache off) and are dropped as soon as access may have changed:
/// <list type="bullet">
/// <item>a save that modifies or deletes a user drops that user, one touching a role, a permission or an image
/// drops everyone (<see cref="IAccessChangeListener"/>, called by the DbContext);</item>
/// <item><see cref="HubConnections"/> revocations (role change, suspension, deletion), including those published
/// by the other instances.</item>
/// </list>
/// Each entry remembers the versions it was read under: a load that raced with an invalidation is never served.
/// Only granted access is cached; a refused user is read again on the next request.
/// </summary>
public sealed class AccessSnapshotCache : IAccessChangeListener
{
    public const string LifetimeKey = "Authorization:AccessCacheSeconds";
    public const int DefaultLifetimeSeconds = 10;
    private const int MaxEntries = 50_000;

    private sealed record Entry(AccessSnapshot Snapshot, Stamp Stamp, DateTimeOffset ExpiresAt);

    /// <summary>The versions a load starts under; pass it back to <see cref="Set"/>.</summary>
    public readonly record struct Stamp(long Global, long User);

    private readonly ConcurrentDictionary<int, Entry> _entries = new();
    private readonly ConcurrentDictionary<int, long> _userVersions = new();
    private readonly TimeProvider _time;
    private long _globalVersion;

    public AccessSnapshotCache(TimeProvider time, TimeSpan lifetime)
    {
        _time = time;
        Lifetime = lifetime < TimeSpan.Zero ? TimeSpan.Zero : lifetime;
    }

    public AccessSnapshotCache(TimeProvider time, IConfiguration configuration)
        : this(time, TimeSpan.FromSeconds(configuration.GetValue(LifetimeKey, DefaultLifetimeSeconds)))
    {
    }

    public TimeSpan Lifetime { get; }

    public int Count => _entries.Count;

    public Stamp Current(int userId) => new(Interlocked.Read(ref _globalVersion), _userVersions.GetValueOrDefault(userId));

    public bool TryGet(int userId, out AccessSnapshot snapshot)
    {
        snapshot = null!;
        if (Lifetime == TimeSpan.Zero || !_entries.TryGetValue(userId, out var entry))
            return false;
        if (entry.Stamp != Current(userId) || _time.GetUtcNow() >= entry.ExpiresAt)
        {
            _entries.TryRemove(new KeyValuePair<int, Entry>(userId, entry));
            return false;
        }
        snapshot = entry.Snapshot;
        return true;
    }

    /// <summary>Caches a snapshot loaded under <paramref name="stamp"/> (taken before the load).</summary>
    public void Set(int userId, AccessSnapshot snapshot, Stamp stamp)
    {
        if (Lifetime == TimeSpan.Zero || stamp != Current(userId))
            return;
        if (_entries.Count >= MaxEntries)
            Trim();
        _entries[userId] = new Entry(snapshot, stamp, _time.GetUtcNow() + Lifetime);
    }

    public void Invalidate(IEnumerable<int> userIds)
    {
        foreach (var userId in userIds)
        {
            _userVersions.AddOrUpdate(userId, 1, (_, version) => version + 1);
            _entries.TryRemove(userId, out _);
        }
        // The version map only grows with distinct changed users; past the cap start over with a new global version.
        if (_userVersions.Count > MaxEntries)
            InvalidateAll();
    }

    public void InvalidateAll()
    {
        Interlocked.Increment(ref _globalVersion);
        _entries.Clear();
        _userVersions.Clear();
    }

    void IAccessChangeListener.UsersChanged(IReadOnlyCollection<int> userIds) => Invalidate(userIds);

    void IAccessChangeListener.AllChanged() => InvalidateAll();

    private void Trim()
    {
        var now = _time.GetUtcNow();
        foreach (var (userId, entry) in _entries)
            if (now >= entry.ExpiresAt)
                _entries.TryRemove(new KeyValuePair<int, Entry>(userId, entry));
        if (_entries.Count >= MaxEntries)
            _entries.Clear();
    }
}
