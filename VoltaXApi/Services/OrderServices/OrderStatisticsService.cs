using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Counts and recharged amounts of the recharge orders (every status), by business day or month of the recharge
    /// date. A series is one GROUP BY on the period of the recharge date (see <see cref="BusinessPeriods.IndexOf{T}"/>).
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
        private sealed record Bucket(int Period, int Count, decimal Amount);

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

        /// <summary>One GROUP BY on the period of the recharge date (a CASE over the periods' boundaries).</summary>
        private async Task<List<T>> PerPeriod<T>(IReadOnlyList<BusinessPeriod> periods, T zero, Func<T, Bucket, T> add)
        {
            var boundaries = BusinessPeriods.Boundaries(periods);
            var from = boundaries[0];
            var to = boundaries[^1];
            var buckets = await Orders
                .Where(o => o.RechargeDate >= from && o.RechargeDate < to)
                .GroupBy(BusinessPeriods.IndexOf<Order>(o => o.RechargeDate, boundaries))
                .Select(g => new Bucket(g.Key, g.Count(), g.Sum(o => o.Amount)))
                .ToListAsync();

            var values = periods.Select(_ => zero).ToList();
            foreach (var bucket in buckets.Where(b => b.Period >= 0 && b.Period < periods.Count))
                values[bucket.Period] = add(values[bucket.Period], bucket);
            return values;
        }
    }
}
