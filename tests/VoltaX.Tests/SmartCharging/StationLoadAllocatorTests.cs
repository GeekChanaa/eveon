using VoltaXApi.Models;
using VoltaXApi.SmartCharging;

namespace VoltaX.Tests.SmartCharging;

/// <summary>Sharing a station's current between its charging sessions, for both strategies.</summary>
public class StationLoadAllocatorTests
{
    private static readonly DateTime T0 = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);

    private static LoadLimitSettings Settings(double? maxA, LoadBalancingStrategyEnum strategy = LoadBalancingStrategyEnum.EqualShare, double min = 6,
        double? maxKW = null, int phases = 3, double margin = 0) => new(maxA, maxKW, phases, 230, min, strategy, margin);

    /// <summary>Sessions 1..n started a minute apart, each with an optional EV maximum current.</summary>
    private static List<SessionDemand> Sessions(params double?[] caps) => caps.Select((c, i) => new SessionDemand(i + 1, T0.AddMinutes(i), c)).ToList();

    private static double A(AllocationResult r, int id) => r.Sessions.Single(s => s.TransactionId == id).AllocatedA;
    private static bool Queued(AllocationResult r, int id) => r.Sessions.Single(s => s.TransactionId == id).Queued;

    [Fact]
    public void The_station_limit_is_the_lower_of_current_and_power_less_the_safety_margin()
    {
        Assert.Equal(32, StationLoadAllocator.EffectiveLimitA(Settings(32)), 6);
        Assert.Equal(31.8, StationLoadAllocator.EffectiveLimitA(Settings(null, maxKW: 22)), 6); // 22 kW / (230 V x 3), rounded down
        Assert.Equal(20, StationLoadAllocator.EffectiveLimitA(Settings(20, maxKW: 22)), 6);
        Assert.Equal(28.8, StationLoadAllocator.EffectiveLimitA(Settings(32, margin: 10)), 6);
        Assert.Equal(0, StationLoadAllocator.EffectiveLimitA(Settings(null)), 6);
        Assert.Equal(11.04, StationLoadAllocator.ToKW(16, Settings(32)), 6);                   // 16 A x 230 V x 3
    }

    [Fact]
    public void Equal_share_splits_the_limit_rounded_down()
    {
        var r = StationLoadAllocator.Allocate(Settings(32), Sessions(null, null, null));
        Assert.All(r.Sessions, s => { Assert.Equal(10.6, s.AllocatedA, 6); Assert.False(s.Queued); });
    }

    [Fact]
    public void Equal_share_queues_the_latest_sessions_that_would_go_below_the_minimum()
    {
        var r = StationLoadAllocator.Allocate(Settings(32), Sessions(null, null, null, null, null, null));
        Assert.All(Enumerable.Range(1, 5), i => Assert.Equal(6.4, A(r, i), 6));
        Assert.True(Queued(r, 6));
        Assert.Equal(0, A(r, 6));

        r = StationLoadAllocator.Allocate(Settings(10), Sessions(null, null));
        Assert.Equal(10, A(r, 1), 6); // the first started keeps charging
        Assert.True(Queued(r, 2));
    }

    [Fact]
    public void Equal_share_gives_what_a_capped_EV_leaves_to_the_others()
    {
        var r = StationLoadAllocator.Allocate(Settings(32), Sessions(10, null, null));
        Assert.Equal(10, A(r, 1), 6);
        Assert.Equal(11, A(r, 2), 6);
        Assert.Equal(11, A(r, 3), 6);

        // An EV capped below the minimum needs only its maximum to be admitted.
        r = StationLoadAllocator.Allocate(Settings(13), Sessions(2, null, null));
        Assert.Equal(2, A(r, 1), 6);
        Assert.Equal(11, A(r, 2), 6);
        Assert.True(Queued(r, 3));
    }

    [Fact]
    public void No_session_or_no_current_means_no_allocation()
    {
        var empty = StationLoadAllocator.Allocate(Settings(32), Sessions());
        Assert.Empty(empty.Sessions);
        Assert.Equal(0, empty.TotalA);

        Assert.All(StationLoadAllocator.Allocate(Settings(0), Sessions(null, null)).Sessions, s => { Assert.True(s.Queued); Assert.Equal(0, s.AllocatedA); });
    }

    [Fact]
    public void Equal_share_admits_by_start_time_not_by_list_order()
    {
        var sessions = new List<SessionDemand> { new(7, T0.AddMinutes(5)), new(3, T0), new(9, T0.AddMinutes(1)) };
        var r = StationLoadAllocator.Allocate(Settings(12), sessions);

        Assert.False(Queued(r, 3));
        Assert.False(Queued(r, 9));
        Assert.True(Queued(r, 7));
    }

    [Fact]
    public void First_come_first_served_fills_sessions_in_start_order()
    {
        var r = StationLoadAllocator.Allocate(Settings(32, LoadBalancingStrategyEnum.FirstComeFirstServed), Sessions(20, null, null));
        Assert.Equal(20, A(r, 1), 6); // its EV maximum
        Assert.Equal(12, A(r, 2), 6); // what is left
        Assert.True(Queued(r, 3));

        // 4 A left: too few for an uncapped session (6 A minimum) but enough for a 4 A EV.
        r = StationLoadAllocator.Allocate(Settings(32, LoadBalancingStrategyEnum.FirstComeFirstServed), Sessions(28, null, 4));
        Assert.Equal(28, A(r, 1), 6);
        Assert.True(Queued(r, 2));
        Assert.Equal(4, A(r, 3), 6);
    }

    [Fact]
    public void Random_session_sets_never_exceed_the_limit_and_equal_share_stays_fair()
    {
        var random = new Random(20261004);
        for (var i = 0; i < 20_000; i++)
        {
            var strategy = random.Next(2) == 0 ? LoadBalancingStrategyEnum.EqualShare : LoadBalancingStrategyEnum.FirstComeFirstServed;
            var settings = new LoadLimitSettings(
                random.Next(4) == 0 ? null : Math.Round(random.NextDouble() * 250, 2),
                random.Next(3) == 0 ? Math.Round(random.NextDouble() * 150, 2) : null,
                random.Next(2) == 0 ? 1 : 3,
                random.Next(5) == 0 ? 120 + random.Next(300) : 230,
                Math.Round(random.NextDouble() * 12, 1),
                strategy,
                random.Next(3) == 0 ? random.NextDouble() * 40 : 0);
            var sessions = Enumerable.Range(1, random.Next(0, 40))
                .Select(id => new SessionDemand(id, T0.AddSeconds(random.Next(0, 3600)), random.Next(3) == 0 ? Math.Round(random.NextDouble() * 40, 1) : null))
                .ToList();

            var result = StationLoadAllocator.Allocate(settings, sessions); // also runs CheckInvariants
            var limit = StationLoadAllocator.EffectiveLimitA(settings);

            Assert.True(result.TotalA <= limit + 1e-3, $"case {i}: {result.TotalA} A above the {limit} A limit");
            Assert.False(result.Sessions.Any(s => s.AllocatedA < 0 || s.Queued && s.AllocatedA != 0), $"case {i}: invalid allocation");
            if (strategy != LoadBalancingStrategyEnum.EqualShare || sessions.Any(s => s.MaxCurrentA != null))
                continue;

            var active = result.Sessions.Where(s => !s.Queued).Select(s => s.AllocatedA).ToList();
            Assert.False(active.Count > 0 && active.Max() - active.Min() > 1e-6, $"case {i}: equal shares differ");
            Assert.False(active.Any(a => a + 1e-6 < settings.MinPerSessionA), $"case {i}: a share below the minimum");
            var expected = settings.MinPerSessionA <= 0 ? sessions.Count : Math.Min(sessions.Count, (int)Math.Floor((limit + 1e-3) / settings.MinPerSessionA));
            Assert.True(active.Count == expected, $"case {i}: admitted {active.Count}, expected {expected}");
            DateTime Start(SessionAllocation s) => sessions.Single(x => x.TransactionId == s.TransactionId).StartedAt;
            var lastAdmitted = result.Sessions.Where(s => !s.Queued).Select(Start).DefaultIfEmpty(DateTime.MinValue).Max();
            var firstQueued = result.Sessions.Where(s => s.Queued).Select(Start).DefaultIfEmpty(DateTime.MaxValue).Min();
            Assert.False(firstQueued < lastAdmitted, $"case {i}: an earlier session was queued");
        }
    }

    [Fact]
    public void The_invariant_check_rejects_an_allocation_above_the_limit()
    {
        var overLimit = new AllocationResult(10, new[] { new SessionAllocation(1, 6, false), new SessionAllocation(2, 6, false) });
        Assert.Throws<InvalidOperationException>(() => StationLoadAllocator.CheckInvariants(overLimit, Sessions(null, null), 6));
    }
}
