using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    /// <summary>Count, charged minutes, charged energy and revenue (VAT included) of a set of charging sessions.</summary>
    public sealed record SessionTotals(int Count, double ChargedMinutes, double ChargedKwhs, decimal Revenue)
    {
        public static readonly SessionTotals Empty = new(0, 0, 0, 0);

        public SessionTotals Add(SessionTotals other) =>
            new(Count + other.Count, ChargedMinutes + other.ChargedMinutes, ChargedKwhs + other.ChargedKwhs, Revenue + other.Revenue);
    }

    /// <summary>VAT rate and idle grace period used to price sessions billed before their amounts were stored.</summary>
    public readonly record struct SessionPricing(decimal VatRate, int GracePeriodSeconds)
    {
        public SessionPricing(decimal vatRate, double gracePeriodSeconds) : this(vatRate, (int)gracePeriodSeconds) { }
    }

    /// <summary>
    /// Charging session aggregates computed by the database instead of loading the sessions.
    /// <para>
    /// Revenue is <see cref="ChargingSession.TotalPriceWithVAT"/>: for a billed session (TotalAmount set) the sum of
    /// its stored energy, time, flat fee and idle amounts, which the database adds up; sessions without stored
    /// amounts (billed on time only, before the amounts existed) are read one by one and priced by the model.
    /// </para>
    /// <para>
    /// <see cref="ByStartAsync{TKey}"/> groups by the business period (day, month) of the session start with one
    /// GROUP BY on a CASE over the periods' UTC boundaries (see <see cref="BusinessPeriods.IndexOf{T}"/>).
    /// </para>
    /// </summary>
    public static class SessionAggregation
    {
        private sealed record Row(int Period, int Count, double ChargedMinutes, double ChargedKwhs,
            decimal EnergyAmount, decimal TimeAmount, decimal FlatFeeAmount, decimal IdleAmount)
        {
            public SessionTotals Totals => new(Count, ChargedMinutes, ChargedKwhs, EnergyAmount + TimeAmount + FlatFeeAmount + IdleAmount);
        }

        public static async Task<SessionTotals> TotalAsync(IQueryable<ChargingSession> sessions, SessionPricing? pricing,
            CancellationToken cancellationToken = default)
        {
            var row = await sessions
                .GroupBy(s => 1)
                .Select(g => new Row(0,
                    g.Count(),
                    g.Sum(s => s.ChargedMinutes ?? 0),
                    g.Sum(s => s.ChargedKwhs ?? 0),
                    g.Sum(s => s.TotalAmount != null ? s.EnergyAmount : null) ?? 0,
                    g.Sum(s => s.TotalAmount != null ? s.TimeAmount : null) ?? 0,
                    g.Sum(s => s.TotalAmount != null ? s.FlatFeeAmount : null) ?? 0,
                    g.Sum(s => s.TotalAmount != null ? s.IdleAmount : null) ?? 0))
                .FirstOrDefaultAsync(cancellationToken);
            if (row == null)
                return SessionTotals.Empty;

            var unbilled = pricing == null
                ? 0m
                : (await UnbilledAsync(sessions, cancellationToken)).Sum(s => s.TotalPriceWithVAT(pricing.Value.VatRate, pricing.Value.GracePeriodSeconds));
            return row.Totals with { Revenue = row.Totals.Revenue + unbilled };
        }

        /// <summary>
        /// Totals by key of the session start, for sessions that start at or after <c>boundaries[0]</c> (the caller
        /// filters them). Each period [boundaries[i], boundaries[i + 1]) is keyed by <paramref name="keyOf"/> of its
        /// start; the rare sessions that start after the last boundary (a charger clock ahead) are read one by one and
        /// keyed by their own start. Only keys with sessions are returned, in chronological order.
        /// </summary>
        public static async Task<Dictionary<TKey, SessionTotals>> ByStartAsync<TKey>(IQueryable<ChargingSession> sessions,
            IReadOnlyList<DateTime> boundaries, Func<DateTime, TKey> keyOf, SessionPricing? pricing,
            CancellationToken cancellationToken = default) where TKey : notnull
        {
            var beyond = boundaries[^1];
            var rows = await sessions
                .Where(s => s.StartDate < beyond)
                .GroupBy(BusinessPeriods.IndexOf<ChargingSession>(s => s.StartDate, boundaries))
                .Select(g => new Row(g.Key,
                    g.Count(),
                    g.Sum(s => s.ChargedMinutes ?? 0),
                    g.Sum(s => s.ChargedKwhs ?? 0),
                    g.Sum(s => s.TotalAmount != null ? s.EnergyAmount : null) ?? 0,
                    g.Sum(s => s.TotalAmount != null ? s.TimeAmount : null) ?? 0,
                    g.Sum(s => s.TotalAmount != null ? s.FlatFeeAmount : null) ?? 0,
                    g.Sum(s => s.TotalAmount != null ? s.IdleAmount : null) ?? 0))
                .ToListAsync(cancellationToken);

            var totals = new Dictionary<TKey, SessionTotals>();
            void Add(TKey key, SessionTotals value) =>
                totals[key] = totals.TryGetValue(key, out var sum) ? sum.Add(value) : value;

            foreach (var row in rows.Where(r => r.Period >= 0).OrderBy(r => r.Period))
                Add(keyOf(boundaries[row.Period]), row.Totals);
            if (pricing != null)
                foreach (var session in await UnbilledAsync(sessions.Where(s => s.StartDate < beyond), cancellationToken))
                {
                    var period = BusinessPeriods.IndexOf(boundaries, session.StartDate);
                    if (period >= 0)
                        Add(keyOf(boundaries[period]), SessionTotals.Empty with { Revenue = Price(session, pricing.Value) });
                }
            foreach (var session in await PricedAsync(sessions.Where(s => s.StartDate >= beyond).OrderBy(s => s.StartDate), cancellationToken))
                Add(keyOf(session.StartDate), new SessionTotals(1, session.ChargedMinutes ?? 0, session.ChargedKwhs ?? 0,
                    pricing == null ? 0 : Price(session, pricing.Value)));
            return totals;
        }

        /// <summary>Revenue per charging station of the sessions' charge points (stations without sessions are absent).</summary>
        public static async Task<Dictionary<int, decimal>> RevenueByStationAsync(IQueryable<ChargingSession> sessions, SessionPricing pricing,
            CancellationToken cancellationToken = default)
        {
            var billed = await sessions
                .Where(s => s.TotalAmount != null)
                .GroupBy(s => (int?)s.Connector!.ChargePoint!.ChargingStationID)
                .Select(g => new
                {
                    StationId = g.Key,
                    EnergyAmount = g.Sum(s => s.EnergyAmount) ?? 0,
                    TimeAmount = g.Sum(s => s.TimeAmount) ?? 0,
                    FlatFeeAmount = g.Sum(s => s.FlatFeeAmount) ?? 0,
                    IdleAmount = g.Sum(s => s.IdleAmount) ?? 0
                })
                .ToListAsync(cancellationToken);
            var unbilled = await sessions
                .Where(s => s.TotalAmount == null)
                .Select(s => new { StationId = (int?)s.Connector!.ChargePoint!.ChargingStationID, Session = new ChargingSession
                {
                    ChargedMinutes = s.ChargedMinutes,
                    PricePerMinute = s.PricePerMinute,
                    IdleMinutes = s.IdleMinutes,
                    PricePerIdleMinute = s.PricePerIdleMinute,
                    IdleAmount = s.IdleAmount
                } })
                .ToListAsync(cancellationToken);

            var revenue = new Dictionary<int, decimal>();
            foreach (var row in billed.Where(r => r.StationId != null))
                revenue[row.StationId!.Value] = row.EnergyAmount + row.TimeAmount + row.FlatFeeAmount + row.IdleAmount;
            foreach (var row in unbilled.Where(r => r.StationId != null))
                revenue[row.StationId!.Value] = revenue.GetValueOrDefault(row.StationId!.Value) + Price(row.Session, pricing);
            return revenue;
        }

        /// <summary>
        /// The sessions with only their dates, charged minutes and energy, and the columns their price needs, for
        /// callers that bucket them in a way a GROUP BY cannot express.
        /// </summary>
        public static Task<List<ChargingSession>> PricedAsync(IQueryable<ChargingSession> sessions, CancellationToken cancellationToken = default) =>
            sessions
                .Select(s => new ChargingSession
                {
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    ChargedMinutes = s.ChargedMinutes,
                    ChargedKwhs = s.ChargedKwhs,
                    PricePerMinute = s.PricePerMinute,
                    IdleMinutes = s.IdleMinutes,
                    PricePerIdleMinute = s.PricePerIdleMinute,
                    EnergyAmount = s.EnergyAmount,
                    TimeAmount = s.TimeAmount,
                    FlatFeeAmount = s.FlatFeeAmount,
                    IdleAmount = s.IdleAmount,
                    TotalAmount = s.TotalAmount
                })
                .ToListAsync(cancellationToken);

        /// <summary>Sessions without stored amounts, with only the columns their price needs.</summary>
        private static Task<List<ChargingSession>> UnbilledAsync(IQueryable<ChargingSession> sessions, CancellationToken cancellationToken) =>
            PricedAsync(sessions.Where(s => s.TotalAmount == null), cancellationToken);

        private static decimal Price(ChargingSession session, SessionPricing pricing) =>
            session.TotalPriceWithVAT(pricing.VatRate, pricing.GracePeriodSeconds);
    }
}
