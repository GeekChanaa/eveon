using VoltaXApi.Models;
using VoltaXApi.Data;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Statistics of the ended charging sessions on a partner's stations, aggregated by the database (see
    /// <see cref="SessionAggregation"/>). "Today", daily and monthly figures use business-time-zone calendar days
    /// (see <see cref="IBusinessClock"/>).
    /// </summary>
    public class PartnerStatisticsService : IPartnerStatisticsService
    {
        private readonly VoltaXApiDbContext _context;
        private readonly IBusinessClock _clock;

        public PartnerStatisticsService(VoltaXApiDbContext context, IBusinessClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public async Task<double> GetTotalRevenueForPartnerAsync(int partnerID, decimal vatRate, double gracePeriodSeconds) =>
            (double)(await Total(Sessions(partnerID), new SessionPricing(vatRate, gracePeriodSeconds))).Revenue;

        public async Task<double> GetTotalRevenueForPartnerTodayAsync(int partnerID, decimal vatRate, double gracePeriodSeconds) =>
            (double)(await Total(Today(Sessions(partnerID)), new SessionPricing(vatRate, gracePeriodSeconds))).Revenue;

        public Task<Dictionary<DateTime, double>> GetDailyRevenueForPartnerLast30DaysAsync(int partnerID, decimal vatRate, double gracePeriodSeconds) =>
            ByDay(Last30Days(Sessions(partnerID)), new SessionPricing(vatRate, gracePeriodSeconds), t => (double)t.Revenue);

        public Task<Dictionary<string, double>> GetMonthlyRevenueForPartnerLastYearAsync(int partnerID, decimal vatRate, double gracePeriodSeconds) =>
            ByMonth(LastYear(Sessions(partnerID)), new SessionPricing(vatRate, gracePeriodSeconds), t => (double)t.Revenue);

        public async Task<double> GetTotalChargingSessionsForPartnerAsync(int partnerID) =>
            await Sessions(partnerID).CountAsync();

        public async Task<double> GetTotalChargingSessionsForPartnerTodayAsync(int partnerID) =>
            await Today(Sessions(partnerID)).CountAsync();

        public Task<Dictionary<DateTime, double>> GetDailyChargingSessionsForPartnerLast30DaysAsync(int partnerID) =>
            ByDay(Last30Days(Sessions(partnerID)), null, t => t.Count);

        public Task<Dictionary<string, double>> GetMonthlyChargingSessionsForPartnerLastYearAsync(int partnerID) =>
            ByMonth(LastYear(Sessions(partnerID)), null, t => t.Count);

        public async Task<double> GetTotalChargedMinutesForPartnerAsync(int partnerID) =>
            await Sessions(partnerID).SumAsync(s => s.ChargedMinutes ?? 0);

        public async Task<double> GetTotalChargedMinutesForPartnerTodayAsync(int partnerID) =>
            await Today(Sessions(partnerID)).SumAsync(s => s.ChargedMinutes ?? 0);

        public Task<Dictionary<DateTime, double>> GetDailyChargedMinutesForPartnerLast30DaysAsync(int partnerID) =>
            ByDay(Last30Days(Sessions(partnerID)), null, t => t.ChargedMinutes);

        public Task<Dictionary<string, double>> GetMonthlyChargedMinutesForPartnerLastYearAsync(int partnerID) =>
            ByMonth(LastYear(Sessions(partnerID)), null, t => t.ChargedMinutes);

        public async Task<double> GetTotalEnergyConsumedForPartnerAsync(int partnerID) =>
            await Sessions(partnerID).SumAsync(s => s.ChargedKwhs ?? 0);

        public async Task<double> GetTotalEnergyConsumedForPartnerTodayAsync(int partnerID) =>
            await Today(Sessions(partnerID)).SumAsync(s => s.ChargedKwhs ?? 0);

        public Task<Dictionary<DateTime, double>> GetDailyEnergyConsumedForPartnerLast30DaysAsync(int partnerID) =>
            ByDay(Last30Days(Sessions(partnerID)), null, t => t.ChargedKwhs);

        public Task<Dictionary<string, double>> GetMonthlyEnergyConsumedForPartnerLastYearAsync(int partnerID) =>
            ByMonth(LastYear(Sessions(partnerID)), null, t => t.ChargedKwhs);

        private IQueryable<ChargingSession> Sessions(int partnerID) =>
            _context.ChargingSessions.AsNoTracking()
                .Where(s => s.EndDate != null && s.Connector!.ChargePoint!.ChargingStation!.PartnerID == partnerID);

        private IQueryable<ChargingSession> Today(IQueryable<ChargingSession> query)
        {
            var from = _clock.StartOfDayUtc(_clock.Today);
            var to = _clock.StartOfDayUtc(_clock.Today.AddDays(1));
            return query.Where(s => s.StartDate >= from && s.StartDate < to);
        }

        private IQueryable<ChargingSession> Last30Days(IQueryable<ChargingSession> query)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddDays(-30));
            return query.Where(s => s.StartDate >= from);
        }

        private IQueryable<ChargingSession> LastYear(IQueryable<ChargingSession> query)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddYears(-1));
            return query.Where(s => s.StartDate >= from);
        }

        private static Task<SessionTotals> Total(IQueryable<ChargingSession> sessions, SessionPricing? pricing) =>
            SessionAggregation.TotalAsync(sessions, pricing);

        // The queries come from Last30Days / LastYear, which start at the first boundary.
        private Task<Dictionary<DateTime, double>> ByDay(IQueryable<ChargingSession> sessions, SessionPricing? pricing, Func<SessionTotals, double> value) =>
            By(sessions, BusinessPeriods.DayBoundaries(_clock, _clock.StartOfDayUtc(_clock.Today.AddDays(-30))), pricing, local => local.Date, value);

        private Task<Dictionary<string, double>> ByMonth(IQueryable<ChargingSession> sessions, SessionPricing? pricing, Func<SessionTotals, double> value) =>
            By(sessions, BusinessPeriods.MonthBoundaries(_clock, _clock.StartOfDayUtc(_clock.Today.AddYears(-1))), pricing,
                local => $"{local.Month}-{local.Year}", value);

        private async Task<Dictionary<TKey, double>> By<TKey>(IQueryable<ChargingSession> sessions, IReadOnlyList<DateTime> boundaries,
            SessionPricing? pricing, Func<DateTime, TKey> key, Func<SessionTotals, double> value) where TKey : notnull
        {
            var totals = await SessionAggregation.ByStartAsync(sessions, boundaries, start => key(_clock.ToLocal(start)), pricing);
            return totals.ToDictionary(t => t.Key, t => value(t.Value));
        }
    }
}
