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
    /// <see cref="ByStartAsync"/> groups by the UTC hour of the session start (by minute when the business time
    /// zone has an offset that is not a whole number of hours), which the caller folds into business days or
    /// months: every session of a bucket falls on the same business date.
    /// </para>
    /// </summary>
    public static class SessionAggregation
    {
        private sealed record Row(int Year, int Month, int Day, int Hour, int Minute, int Count, double ChargedMinutes, double ChargedKwhs,
            decimal EnergyAmount, decimal TimeAmount, decimal FlatFeeAmount, decimal IdleAmount);

        public static async Task<SessionTotals> TotalAsync(IQueryable<ChargingSession> sessions, SessionPricing? pricing,
            CancellationToken cancellationToken = default)
        {
            var row = await sessions
                .GroupBy(s => 1)
                .Select(g => new Row(0, 0, 0, 0, 0,
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
            return Totals(row) with { Revenue = BilledRevenue(row) + unbilled };
        }

        /// <summary>Totals per start instant bucket (UTC, ascending); see the class remarks.</summary>
        public static async Task<IReadOnlyList<(DateTime StartUtc, SessionTotals Totals)>> ByStartAsync(IQueryable<ChargingSession> sessions,
            TimeZoneInfo businessZone, SessionPricing? pricing, CancellationToken cancellationToken = default)
        {
            var byMinute = !HasWholeHourOffsets(businessZone);
            var grouped = byMinute
                ? sessions.GroupBy(s => new { s.StartDate.Year, s.StartDate.Month, s.StartDate.Day, s.StartDate.Hour, s.StartDate.Minute })
                    .Select(g => new Row(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, g.Key.Minute,
                        g.Count(),
                        g.Sum(s => s.ChargedMinutes ?? 0),
                        g.Sum(s => s.ChargedKwhs ?? 0),
                        g.Sum(s => s.TotalAmount != null ? s.EnergyAmount : null) ?? 0,
                        g.Sum(s => s.TotalAmount != null ? s.TimeAmount : null) ?? 0,
                        g.Sum(s => s.TotalAmount != null ? s.FlatFeeAmount : null) ?? 0,
                        g.Sum(s => s.TotalAmount != null ? s.IdleAmount : null) ?? 0))
                : sessions.GroupBy(s => new { s.StartDate.Year, s.StartDate.Month, s.StartDate.Day, s.StartDate.Hour })
                    .Select(g => new Row(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0,
                        g.Count(),
                        g.Sum(s => s.ChargedMinutes ?? 0),
                        g.Sum(s => s.ChargedKwhs ?? 0),
                        g.Sum(s => s.TotalAmount != null ? s.EnergyAmount : null) ?? 0,
                        g.Sum(s => s.TotalAmount != null ? s.TimeAmount : null) ?? 0,
                        g.Sum(s => s.TotalAmount != null ? s.FlatFeeAmount : null) ?? 0,
                        g.Sum(s => s.TotalAmount != null ? s.IdleAmount : null) ?? 0));

            var buckets = (await grouped.ToListAsync(cancellationToken))
                .ToDictionary(r => new DateTime(r.Year, r.Month, r.Day, r.Hour, r.Minute, 0, DateTimeKind.Utc), r => Totals(r) with { Revenue = BilledRevenue(r) });

            if (pricing != null)
            {
                foreach (var session in await UnbilledAsync(sessions, cancellationToken))
                {
                    var start = session.StartDate;
                    var key = new DateTime(start.Year, start.Month, start.Day, start.Hour, byMinute ? start.Minute : 0, 0, DateTimeKind.Utc);
                    // Present: the grouped query counted every session, priced or not.
                    if (buckets.TryGetValue(key, out var totals))
                        buckets[key] = totals with { Revenue = totals.Revenue + session.TotalPriceWithVAT(pricing.Value.VatRate, pricing.Value.GracePeriodSeconds) };
                }
            }

            return buckets.OrderBy(b => b.Key).Select(b => (b.Key, b.Value)).ToList();
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
                revenue[row.StationId!.Value] = revenue.GetValueOrDefault(row.StationId!.Value)
                    + row.Session.TotalPriceWithVAT(pricing.VatRate, pricing.GracePeriodSeconds);
            return revenue;
        }

        /// <summary>
        /// The sessions with only their dates and the columns their price needs, for callers that bucket them in a way
        /// a GROUP BY cannot express.
        /// </summary>
        public static Task<List<ChargingSession>> PricedAsync(IQueryable<ChargingSession> sessions, CancellationToken cancellationToken = default) =>
            sessions
                .Select(s => new ChargingSession
                {
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    ChargedMinutes = s.ChargedMinutes,
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

        private static SessionTotals Totals(Row row) => new(row.Count, row.ChargedMinutes, row.ChargedKwhs, 0);

        private static decimal BilledRevenue(Row row) => row.EnergyAmount + row.TimeAmount + row.FlatFeeAmount + row.IdleAmount;

        /// <summary>True when every UTC hour falls on a single business date (no half-hour or 45-minute offsets).</summary>
        public static bool HasWholeHourOffsets(TimeZoneInfo zone) =>
            zone.BaseUtcOffset.Ticks % TimeSpan.TicksPerHour == 0
            && zone.GetAdjustmentRules().All(rule => rule.DaylightDelta.Ticks % TimeSpan.TicksPerHour == 0
                && rule.BaseUtcOffsetDelta.Ticks % TimeSpan.TicksPerHour == 0);
    }
}
