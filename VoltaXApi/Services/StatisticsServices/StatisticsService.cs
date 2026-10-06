using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Services
{
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

        public async Task<double> GetTotalRevenueAsync(double vatRate, double gracePeriodSeconds)
        {
            var sessions = await PricedSessions(_context.ChargingSessions.Where(s => s.EndDate != null));
            return sessions.Sum(s => s.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds));
        }

        // Total revenue for the current business day
        public async Task<double> GetTotalRevenueTodayAsync(double vatRate, double gracePeriodSeconds)
        {
            var from = _clock.StartOfDayUtc(_clock.Today);
            var to = _clock.StartOfDayUtc(_clock.Today.AddDays(1));
            var sessions = await PricedSessions(_context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= from && s.StartDate < to));
            return sessions.Sum(s => s.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds));
        }

        // Daily revenue for the last 30 business days, keyed by business date
        public async Task<Dictionary<DateTime, double>> GetDailyRevenueLast30DaysAsync(double vatRate, double gracePeriodSeconds)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddDays(-30));
            var sessions = await PricedSessions(_context.ChargingSessions.Where(s => s.EndDate != null && s.StartDate >= from));

            return sessions
                .GroupBy(s => _clock.ToLocal(s.StartDate).Date)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds)));
        }

        // Monthly revenue for the last year, keyed "month-year" of the business date
        public async Task<Dictionary<string, double>> GetMonthlyRevenueLastYearAsync(double vatRate, double gracePeriodSeconds)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddYears(-1));
            var sessions = await PricedSessions(_context.ChargingSessions.Where(s => s.EndDate != null && s.StartDate >= from));

            return sessions
                .GroupBy(s => MonthKey(_clock.ToLocal(s.StartDate)))
                .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds)));
        }

        private static string MonthKey(DateTime local) => $"{local.Month}-{local.Year}";

        /// <summary>Loads only the columns the price computation needs.</summary>
        private static Task<List<ChargingSession>> PricedSessions(IQueryable<ChargingSession> query) =>
            query.AsNoTracking()
                .Select(s => new ChargingSession
                {
                    StartDate = s.StartDate,
                    ChargedMinutes = s.ChargedMinutes,
                    PricePerMinute = s.PricePerMinute,
                    IdleMinutes = s.IdleMinutes,
                    PricePerIdleMinute = s.PricePerIdleMinute
                })
                .ToListAsync();
    }
}
