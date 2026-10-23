using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Counts and recharged amounts of the recharge orders (every status), by business day or month of the recharge
    /// date. A series is one GROUP BY on the UTC hour (by minute when the business time zone has an offset that is
    /// not a whole number of hours), folded into the periods.
    /// </summary>
    public interface IOrderStatisticsService
    {
        Task<decimal> RechargedAmountAsync();
        Task<decimal> RechargedAmountTodayAsync();
        /// <summary>Orders recharged between the two instants, both included.</summary>
        Task<decimal> RechargedAmountBetweenAsync(DateTime start, DateTime end);
        /// <summary>One amount per business day, the oldest first and today last.</summary>
        Task<List<decimal>> RechargedAmountPerDayAsync(int days);
        Task<int> CountTodayAsync();
        /// <summary>One count per business day, the oldest first and today last.</summary>
        Task<List<double>> CountPerDayAsync(int days);
        /// <summary>One count per business month, the oldest first and the current month last.</summary>
        Task<List<double>> CountPerMonthAsync(int months);
    }

    public sealed class OrderStatisticsService : IOrderStatisticsService
    {
        private sealed record Bucket(int Year, int Month, int Day, int Hour, int Minute, int Count, decimal Amount)
        {
            public DateTime StartUtc => new(Year, Month, Day, Hour, Minute, 0, DateTimeKind.Utc);
        }

        private readonly VoltaXApiDbContext _context;
        private readonly IBusinessClock _clock;

        public OrderStatisticsService(VoltaXApiDbContext context, IBusinessClock clock)
        {
            _context = context;
            _clock = clock;
        }

        private IQueryable<Order> Orders => _context.Orders.AsNoTracking();

        private BusinessPeriod Today => BusinessPeriods.LastDays(_clock, 1)[0];

        public Task<decimal> RechargedAmountAsync() => Orders.SumAsync(o => o.Amount);

        public Task<decimal> RechargedAmountTodayAsync()
        {
            var today = Today;
            return Orders.Where(o => o.RechargeDate >= today.Start && o.RechargeDate < today.End).SumAsync(o => o.Amount);
        }

        public Task<decimal> RechargedAmountBetweenAsync(DateTime start, DateTime end) =>
            Orders.Where(o => o.RechargeDate >= start && o.RechargeDate <= end).SumAsync(o => o.Amount);

        public Task<int> CountTodayAsync()
        {
            var today = Today;
            return Orders.CountAsync(o => o.RechargeDate >= today.Start && o.RechargeDate < today.End);
        }

        public async Task<List<decimal>> RechargedAmountPerDayAsync(int days) =>
            // 0.0m, like an empty SUM from the database, serializes as 0.0.
            await PerPeriod(BusinessPeriods.LastDays(_clock, days), 0.0m, (sum, bucket) => sum + bucket.Amount);

        public async Task<List<double>> CountPerDayAsync(int days) =>
            await PerPeriod(BusinessPeriods.LastDays(_clock, days), 0d, (count, bucket) => count + bucket.Count);

        public async Task<List<double>> CountPerMonthAsync(int months) =>
            await PerPeriod(BusinessPeriods.LastMonths(_clock, months), 0d, (count, bucket) => count + bucket.Count);

        private async Task<List<T>> PerPeriod<T>(IReadOnlyList<BusinessPeriod> periods, T zero, Func<T, Bucket, T> add)
        {
            var values = periods.Select(_ => zero).ToList();
            foreach (var bucket in await Buckets(periods[0].Start, periods[^1].End))
            {
                var index = BusinessPeriods.IndexOf(periods, bucket.StartUtc);
                if (index >= 0)
                    values[index] = add(values[index], bucket);
            }
            return values;
        }

        private Task<List<Bucket>> Buckets(DateTime from, DateTime to)
        {
            var orders = Orders.Where(o => o.RechargeDate >= from && o.RechargeDate < to);
            return SessionAggregation.HasWholeHourOffsets(_clock.TimeZone)
                ? orders.GroupBy(o => new { o.RechargeDate.Year, o.RechargeDate.Month, o.RechargeDate.Day, o.RechargeDate.Hour })
                    .Select(g => new Bucket(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, g.Count(), g.Sum(o => o.Amount)))
                    .ToListAsync()
                : orders.GroupBy(o => new { o.RechargeDate.Year, o.RechargeDate.Month, o.RechargeDate.Day, o.RechargeDate.Hour, o.RechargeDate.Minute })
                    .Select(g => new Bucket(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, g.Key.Minute, g.Count(), g.Sum(o => o.Amount)))
                    .ToListAsync();
        }
    }
}
