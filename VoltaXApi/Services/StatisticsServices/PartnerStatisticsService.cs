using VoltaXApi.Models;
using VoltaXApi.Data;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Statistics of the ended charging sessions on a partner's stations. "Today", daily and monthly figures
    /// use business-time-zone calendar days (see <see cref="IBusinessClock"/>).
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

        public async Task<double> GetTotalRevenueForPartnerAsync(int partnerID, double vatRate, double gracePeriodSeconds) =>
            (await Load(Sessions(partnerID))).Sum(s => Revenue(s, vatRate, gracePeriodSeconds));

        public async Task<double> GetTotalRevenueForPartnerTodayAsync(int partnerID, double vatRate, double gracePeriodSeconds) =>
            (await Load(Today(Sessions(partnerID)))).Sum(s => Revenue(s, vatRate, gracePeriodSeconds));

        public async Task<Dictionary<DateTime, double>> GetDailyRevenueForPartnerLast30DaysAsync(int partnerID, double vatRate, double gracePeriodSeconds) =>
            ByDay(await Load(Last30Days(Sessions(partnerID))), s => Revenue(s, vatRate, gracePeriodSeconds));

        public async Task<Dictionary<string, double>> GetMonthlyRevenueForPartnerLastYearAsync(int partnerID, double vatRate, double gracePeriodSeconds) =>
            ByMonth(await Load(LastYear(Sessions(partnerID))), s => Revenue(s, vatRate, gracePeriodSeconds));

        public async Task<double> GetTotalChargingSessionsForPartnerAsync(int partnerID) =>
            await Sessions(partnerID).CountAsync();

        public async Task<double> GetTotalChargingSessionsForPartnerTodayAsync(int partnerID) =>
            await Today(Sessions(partnerID)).CountAsync();

        public async Task<Dictionary<DateTime, double>> GetDailyChargingSessionsForPartnerLast30DaysAsync(int partnerID) =>
            ByDay(await Load(Last30Days(Sessions(partnerID))), _ => 1);

        public async Task<Dictionary<string, double>> GetMonthlyChargingSessionsForPartnerLastYearAsync(int partnerID) =>
            ByMonth(await Load(LastYear(Sessions(partnerID))), _ => 1);

        public async Task<double> GetTotalChargedMinutesForPartnerAsync(int partnerID) =>
            await Sessions(partnerID).SumAsync(s => s.ChargedMinutes ?? 0);

        public async Task<double> GetTotalChargedMinutesForPartnerTodayAsync(int partnerID) =>
            await Today(Sessions(partnerID)).SumAsync(s => s.ChargedMinutes ?? 0);

        public async Task<Dictionary<DateTime, double>> GetDailyChargedMinutesForPartnerLast30DaysAsync(int partnerID) =>
            ByDay(await Load(Last30Days(Sessions(partnerID))), s => s.ChargedMinutes ?? 0);

        public async Task<Dictionary<string, double>> GetMonthlyChargedMinutesForPartnerLastYearAsync(int partnerID) =>
            ByMonth(await Load(LastYear(Sessions(partnerID))), s => s.ChargedMinutes ?? 0);

        public async Task<double> GetTotalEnergyConsumedForPartnerAsync(int partnerID) =>
            await Sessions(partnerID).SumAsync(s => s.ChargedKwhs ?? 0);

        public async Task<double> GetTotalEnergyConsumedForPartnerTodayAsync(int partnerID) =>
            await Today(Sessions(partnerID)).SumAsync(s => s.ChargedKwhs ?? 0);

        public async Task<Dictionary<DateTime, double>> GetDailyEnergyConsumedForPartnerLast30DaysAsync(int partnerID) =>
            ByDay(await Load(Last30Days(Sessions(partnerID))), s => s.ChargedKwhs ?? 0);

        public async Task<Dictionary<string, double>> GetMonthlyEnergyConsumedForPartnerLastYearAsync(int partnerID) =>
            ByMonth(await Load(LastYear(Sessions(partnerID))), s => s.ChargedKwhs ?? 0);

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

        /// <summary>Loads only the columns the statistics need.</summary>
        private static Task<List<ChargingSession>> Load(IQueryable<ChargingSession> query) =>
            query.Select(s => new ChargingSession
            {
                StartDate = s.StartDate,
                ChargedMinutes = s.ChargedMinutes,
                ChargedKwhs = s.ChargedKwhs,
                PricePerMinute = s.PricePerMinute,
                IdleMinutes = s.IdleMinutes,
                PricePerIdleMinute = s.PricePerIdleMinute
            }).ToListAsync();

        private static double Revenue(ChargingSession session, double vatRate, double gracePeriodSeconds) =>
            session.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds);

        private Dictionary<DateTime, double> ByDay(IEnumerable<ChargingSession> sessions, Func<ChargingSession, double> value) =>
            sessions.GroupBy(s => _clock.ToLocal(s.StartDate).Date).ToDictionary(g => g.Key, g => g.Sum(value));

        private Dictionary<string, double> ByMonth(IEnumerable<ChargingSession> sessions, Func<ChargingSession, double> value) =>
            sessions.GroupBy(s =>
            {
                var local = _clock.ToLocal(s.StartDate);
                return $"{local.Month}-{local.Year}";
            }).ToDictionary(g => g.Key, g => g.Sum(value));
    }
}
