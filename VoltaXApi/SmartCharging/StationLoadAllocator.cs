using VoltaXApi.Models;

namespace VoltaXApi.SmartCharging
{
    public sealed record LoadLimitSettings(
        double? MaxCurrentA,
        double? MaxPowerKW,
        int Phases,
        double Voltage,
        double MinPerSessionA,
        LoadBalancingStrategyEnum Strategy,
        double SafetyMarginPercent)
    {
        public static LoadLimitSettings From(StationLoadLimit limit) => new(limit.MaxCurrentA, limit.MaxPowerKW, limit.Phases,
            limit.Voltage, limit.MinPerSessionA, limit.Strategy, limit.SafetyMarginPercent);
    }

    /// <summary>A running session; <paramref name="MaxCurrentA"/> is what the EV can take (ISO 15118 charging needs), when known.</summary>
    public sealed record SessionDemand(int TransactionId, DateTime StartedAt, double? MaxCurrentA = null);

    public sealed record SessionAllocation(int TransactionId, double AllocatedA, bool Queued);

    public sealed record AllocationResult(double StationLimitA, IReadOnlyList<SessionAllocation> Sessions)
    {
        public double TotalA => Sessions.Sum(s => s.AllocatedA);
    }

    /// <summary>
    /// Shares a station's current (A per phase) between its running sessions. Pure and deterministic.
    /// <para>EqualShare: sessions are admitted in start order while every admitted one can get the minimum
    /// (or its EV maximum when lower); the admitted ones share the limit equally, an EV maximum below the
    /// share frees the rest for the others. Sessions that cannot get the minimum get 0 A and are queued.</para>
    /// <para>FirstComeFirstServed: in start order each session takes what it can (its EV maximum, else everything left);
    /// a session that cannot get the minimum gets 0 A and is queued.</para>
    /// The result never exceeds the station limit (checked by <see cref="CheckInvariants"/>).
    /// </summary>
    public static class StationLoadAllocator
    {
        private const double Epsilon = 1e-3;

        /// <summary>Station limit in A per phase after the safety margin: the lower of MaxCurrentA and MaxPowerKW / (V x phases).</summary>
        public static double EffectiveLimitA(LoadLimitSettings settings)
        {
            double? limit = settings.MaxCurrentA is > 0 ? settings.MaxCurrentA : null;
            if (settings.MaxPowerKW is > 0 && settings.Voltage > 0 && settings.Phases > 0)
            {
                var fromPower = settings.MaxPowerKW.Value * 1000 / (settings.Voltage * settings.Phases);
                limit = limit == null ? fromPower : Math.Min(limit.Value, fromPower);
            }
            if (limit == null || double.IsNaN(limit.Value) || double.IsInfinity(limit.Value)) return 0;
            var margin = Math.Clamp(settings.SafetyMarginPercent, 0, 100);
            return Floor(limit.Value * (1 - margin / 100));
        }

        /// <summary>kW drawn by a session at <paramref name="currentA"/> per phase.</summary>
        public static double ToKW(double currentA, LoadLimitSettings settings) =>
            Math.Round(currentA * Math.Max(settings.Voltage, 0) * Math.Max(settings.Phases, 1) / 1000, 2);

        public static AllocationResult Allocate(LoadLimitSettings settings, IReadOnlyList<SessionDemand> sessions)
        {
            var limit = EffectiveLimitA(settings);
            var min = Math.Max(0, double.IsNaN(settings.MinPerSessionA) ? 0 : settings.MinPerSessionA);
            var ordered = sessions.OrderBy(s => s.StartedAt).ThenBy(s => s.TransactionId).ToList();
            var allocations = settings.Strategy == LoadBalancingStrategyEnum.FirstComeFirstServed
                ? FirstComeFirstServed(ordered, limit, min)
                : EqualShare(ordered, limit, min);
            var result = new AllocationResult(limit, allocations);
            CheckInvariants(result, sessions, min);
            return result;
        }

        /// <summary>Throws when an allocation breaks the rules; an allocator bug must never reach a charger.</summary>
        public static void CheckInvariants(AllocationResult result, IReadOnlyList<SessionDemand> sessions, double min)
        {
            if (result.Sessions.Count != sessions.Count)
                throw new InvalidOperationException("Load allocation: every session must get exactly one allocation.");
            if (result.TotalA > result.StationLimitA + Epsilon)
                throw new InvalidOperationException($"Load allocation exceeds the station limit ({result.TotalA} A > {result.StationLimitA} A).");
            var caps = sessions.ToDictionary(s => s.TransactionId, s => s.MaxCurrentA);
            foreach (var allocation in result.Sessions)
            {
                if (allocation.AllocatedA < 0 || double.IsNaN(allocation.AllocatedA))
                    throw new InvalidOperationException($"Load allocation: negative current for transaction {allocation.TransactionId}.");
                if (allocation.Queued && allocation.AllocatedA != 0)
                    throw new InvalidOperationException($"Load allocation: queued transaction {allocation.TransactionId} has current.");
                var cap = caps[allocation.TransactionId];
                if (cap != null && allocation.AllocatedA > Math.Max(cap.Value, 0) + Epsilon)
                    throw new InvalidOperationException($"Load allocation: transaction {allocation.TransactionId} gets more than its EV maximum.");
                if (!allocation.Queued && allocation.AllocatedA + Epsilon < Math.Min(min, Floor(Math.Max(cap ?? double.MaxValue, 0))))
                    throw new InvalidOperationException($"Load allocation: transaction {allocation.TransactionId} gets less than the minimum.");
            }
        }

        private static List<SessionAllocation> EqualShare(List<SessionDemand> ordered, double limit, double min)
        {
            // Admission: in start order, while the minimum (or the EV maximum when lower) still fits for everyone admitted.
            var admitted = new List<SessionDemand>();
            var needed = 0.0;
            foreach (var session in ordered)
            {
                var need = Math.Min(min, Cap(session));
                if (needed + need <= limit + Epsilon)
                {
                    admitted.Add(session);
                    needed += need;
                }
            }

            // Water filling: EVs whose maximum is below the equal share keep their maximum, the others share the rest.
            var shares = new Dictionary<int, double>();
            var open = admitted.ToList();
            var remaining = limit;
            while (open.Count > 0)
            {
                var share = remaining / open.Count;
                var settled = open.Where(s => Cap(s) <= share).ToList();
                if (settled.Count == 0)
                {
                    foreach (var session in open) shares[session.TransactionId] = share;
                    break;
                }
                foreach (var session in settled)
                {
                    shares[session.TransactionId] = Cap(session);
                    remaining -= Cap(session);
                    open.Remove(session);
                }
            }

            return ordered.Select(s => shares.TryGetValue(s.TransactionId, out var a)
                    ? new SessionAllocation(s.TransactionId, Floor(a), false)
                    : new SessionAllocation(s.TransactionId, 0, true))
                .ToList();
        }

        private static List<SessionAllocation> FirstComeFirstServed(List<SessionDemand> ordered, double limit, double min)
        {
            var remaining = limit;
            var result = new List<SessionAllocation>();
            foreach (var session in ordered)
            {
                var give = Floor(Math.Min(remaining, Cap(session)));
                if (give + Epsilon < Math.Min(min, Floor(Cap(session))) || give <= 0 && Cap(session) > 0)
                {
                    result.Add(new SessionAllocation(session.TransactionId, 0, true));
                    continue;
                }
                result.Add(new SessionAllocation(session.TransactionId, give, false));
                remaining -= give;
            }
            return result;
        }

        private static double Cap(SessionDemand session) => session.MaxCurrentA is { } cap ? Math.Max(cap, 0) : double.MaxValue;

        /// <summary>Rounds down to 0.1 A (the 1.6 limit resolution), so rounding never adds current.</summary>
        private static double Floor(double value) => value >= double.MaxValue / 20 ? value : Math.Floor(value * 10 + 1e-6) / 10;
    }
}
