using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Operator statistics. Revenue covers ended sessions and is summed by the database (see
    /// <see cref="SessionAggregation"/>); daily and monthly figures use business-time-zone dates of the session start.
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly VoltaXApiDbContext _context;
        private readonly IBusinessClock _clock;

        public StatisticsService(
          VoltaXApiDbContext context,
          IBusinessClock clock
        )
        {
            _context = context;
            _clock = clock;
        }

        public async Task<ChargePointStatisticsSummaryDto> GetChargePointStatisticsSummary(int chargePointID)
        {
            var lastWeek = _clock.UtcNow.AddDays(-7);
            var sessions = _context.ChargingSessions.AsNoTracking().Where(cs => cs.Connector!.ChargePointID == chargePointID);
            var transactions = _context.Transactions.AsNoTracking().Where(t => t.ChargingSession!.Connector!.ChargePointID == chargePointID);

            return new ChargePointStatisticsSummaryDto
            {
                NbrChargingSessions = await sessions.CountAsync(),
                NbrChargingSessionsLastWeek = await sessions.CountAsync(cs => cs.StartDate > lastWeek),
                TotalEnergy = await transactions.SumAsync(t => t.MeterStop - t.MeterStart) ?? 0,
                TotalEnergyLastWeek = await transactions.Where(t => t.StartTime > lastWeek).SumAsync(t => t.MeterStop - t.MeterStart)
            };
        }

        public async Task<double> GetTotalRevenueAsync(decimal vatRate, double gracePeriodSeconds) =>
            (double)(await SessionAggregation.TotalAsync(EndedSessions(), new SessionPricing(vatRate, gracePeriodSeconds))).Revenue;

        // Total revenue for the current business day
        public async Task<double> GetTotalRevenueTodayAsync(decimal vatRate, double gracePeriodSeconds)
        {
            var from = _clock.StartOfDayUtc(_clock.Today);
            var to = _clock.StartOfDayUtc(_clock.Today.AddDays(1));
            var sessions = EndedSessions().Where(s => s.StartDate >= from && s.StartDate < to);
            return (double)(await SessionAggregation.TotalAsync(sessions, new SessionPricing(vatRate, gracePeriodSeconds))).Revenue;
        }

        // Daily revenue for the last 30 business days, keyed by business date
        public async Task<Dictionary<DateTime, double>> GetDailyRevenueLast30DaysAsync(decimal vatRate, double gracePeriodSeconds)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddDays(-30));
            return await RevenueBy(from, BusinessPeriods.DayBoundaries(_clock, from), vatRate, gracePeriodSeconds, local => local.Date);
        }

        // Monthly revenue for the last year, keyed "month-year" of the business date
        public async Task<Dictionary<string, double>> GetMonthlyRevenueLastYearAsync(decimal vatRate, double gracePeriodSeconds)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddYears(-1));
            return await RevenueBy(from, BusinessPeriods.MonthBoundaries(_clock, from), vatRate, gracePeriodSeconds, local => $"{local.Month}-{local.Year}");
        }

        private IQueryable<ChargingSession> EndedSessions() =>
            _context.ChargingSessions.AsNoTracking().Where(s => s.EndDate != null);

        private async Task<Dictionary<TKey, double>> RevenueBy<TKey>(DateTime from, IReadOnlyList<DateTime> boundaries, decimal vatRate,
            double gracePeriodSeconds, Func<DateTime, TKey> key) where TKey : notnull
        {
            var totals = await SessionAggregation.ByStartAsync(EndedSessions().Where(s => s.StartDate >= from), boundaries,
                start => key(_clock.ToLocal(start)), new SessionPricing(vatRate, gracePeriodSeconds));
            return totals.ToDictionary(t => t.Key, t => (double)t.Value.Revenue);
        }
    }
}
