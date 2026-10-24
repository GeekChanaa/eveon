using System.Linq.Expressions;

namespace VoltaXApi.Services
{
    /// <summary>A half-open UTC interval [Start, End) covering business days or months.</summary>
    public readonly record struct BusinessPeriod(DateTime Start, DateTime End)
    {
        public bool Contains(DateTime utc) => utc >= Start && utc < End;
    }

    /// <summary>
    /// Business days and months as UTC boundaries, and the SQL that tells which period a row belongs to. The
    /// boundaries are exact instants, so any business time zone works (half-hour offsets, DST).
    /// </summary>
    public static class BusinessPeriods
    {
        /// <summary>Consecutive business days ending with today, oldest first.</summary>
        public static IReadOnlyList<BusinessPeriod> LastDays(IBusinessClock clock, int count) =>
            Enumerable.Range(0, count)
                .Select(i => clock.Today.AddDays(i - count + 1))
                .Select(day => new BusinessPeriod(clock.StartOfDayUtc(day), clock.StartOfDayUtc(day.AddDays(1))))
                .ToList();

        /// <summary>Consecutive business months ending with the current one, oldest first.</summary>
        public static IReadOnlyList<BusinessPeriod> LastMonths(IBusinessClock clock, int count)
        {
            var current = new DateTime(clock.Today.Year, clock.Today.Month, 1);
            return Enumerable.Range(0, count)
                .Select(i => current.AddMonths(i - count + 1))
                .Select(month => new BusinessPeriod(clock.StartOfMonthUtc(month.Year, month.Month), clock.StartOfDayUtc(month.AddMonths(1))))
                .ToList();
        }

        /// <summary>Starts of the business days from <paramref name="from"/> (a UTC instant) through today, and the end of today.</summary>
        public static IReadOnlyList<DateTime> DayBoundaries(IBusinessClock clock, DateTime from)
        {
            var boundaries = new List<DateTime> { from };
            for (var day = clock.ToLocal(from).Date.AddDays(1); day <= clock.Today.AddDays(1); day = day.AddDays(1))
                boundaries.Add(clock.StartOfDayUtc(day));
            return boundaries;
        }

        /// <summary>
        /// <paramref name="from"/> (a UTC instant), the starts of the business months after it through the current
        /// one, and the end of the current month.
        /// </summary>
        public static IReadOnlyList<DateTime> MonthBoundaries(IBusinessClock clock, DateTime from)
        {
            var boundaries = new List<DateTime> { from };
            var local = clock.ToLocal(from);
            var end = new DateTime(clock.Today.Year, clock.Today.Month, 1).AddMonths(1);
            for (var month = new DateTime(local.Year, local.Month, 1).AddMonths(1); month <= end; month = month.AddMonths(1))
                boundaries.Add(clock.StartOfMonthUtc(month.Year, month.Month));
            return boundaries;
        }

        /// <summary>The boundaries of contiguous periods: every start, then the last end.</summary>
        public static IReadOnlyList<DateTime> Boundaries(IReadOnlyList<BusinessPeriod> periods) =>
            periods.Select(p => p.Start).Append(periods[^1].End).ToList();

        /// <summary>
        /// An expression the database evaluates to i when boundaries[i] &lt;= time &lt; boundaries[i + 1], to -1 before
        /// the first boundary and to boundaries.Count - 1 at or after the last one: a single CASE whose boundaries are
        /// query parameters, to GROUP BY.
        /// </summary>
        public static Expression<Func<T, int>> IndexOf<T>(Expression<Func<T, DateTime>> time, IReadOnlyList<DateTime> boundaries)
        {
            // Read through a field so Entity Framework turns each boundary into a parameter, not a literal.
            var values = Expression.Field(Expression.Constant(new BoundaryValues(boundaries.ToArray())), nameof(BoundaryValues.Values));
            Expression body = Expression.Constant(boundaries.Count - 1);
            for (var i = boundaries.Count - 1; i >= 0; i--)
                body = Expression.Condition(
                    Expression.LessThan(time.Body, Expression.ArrayIndex(values, Expression.Constant(i))),
                    Expression.Constant(i - 1),
                    body);
            return Expression.Lambda<Func<T, int>>(body, time.Parameters);
        }

        /// <summary>Index of the period of <paramref name="utc"/> as <see cref="IndexOf{T}"/> computes it.</summary>
        public static int IndexOf(IReadOnlyList<DateTime> boundaries, DateTime utc)
        {
            for (var i = 0; i < boundaries.Count; i++)
                if (utc < boundaries[i])
                    return i - 1;
            return boundaries.Count - 1;
        }

        private sealed class BoundaryValues(DateTime[] values)
        {
            public readonly DateTime[] Values = values;
        }
    }
}
