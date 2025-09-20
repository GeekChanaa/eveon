using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Services
{
    public class PartnerStatisticsService : IPartnerStatisticsService
    {
        private readonly IChargingSessionRepository _chargingSessionRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly VoltaXApiDbContext _context;
        public PartnerStatisticsService(
          IChargingSessionRepository chargingSessionRepository,
          ITransactionRepository transactionRepository,
          VoltaXApiDbContext context
        )
        {
            _chargingSessionRepo = chargingSessionRepository;
            _transactionRepo = transactionRepository;
            _context = context;
        }


        public async Task<double> GetTotalRevenueForPartnerAsync(int partnerID, double vatRate, double gracePeriodSeconds)
        {
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            double totalRevenue = 0;
            foreach (var session in sessions)
            {
                totalRevenue += session.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds);
            }

            return totalRevenue;
        }

        // Total revenue for today
        public async Task<double> GetTotalRevenueForPartnerTodayAsync(int partnerID, double vatRate, double gracePeriodSeconds)
        {
            var today = DateTime.Today;

            var sessions = await _context.ChargingSessions
            .Where(s => s.EndDate != null && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID
                    && s.StartDate >= today
                    && s.StartDate < today.AddDays(1))
            .ToListAsync();

            double totalRevenue = 0;
            foreach (var session in sessions)
            {
                totalRevenue += session.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds);
            }

            return totalRevenue;
        }

        // Daily revenue for last 30 days
        public async Task<Dictionary<DateTime, double>> GetDailyRevenueForPartnerLast30DaysAsync(int partnerID, double vatRate, double gracePeriodSeconds)
        {
            var startDate = DateTime.Today.AddDays(-30);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate)
                .ToListAsync();

            var revenueByDay = new Dictionary<DateTime, double>();
            foreach (var session in sessions)
            {
                var date = session.StartDate;
                var revenue = session.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds);

                if (revenueByDay.ContainsKey(date))
                    revenueByDay[date] += revenue;
                else
                    revenueByDay[date] = revenue;
            }

            return revenueByDay;
        }

        // Monthly revenue for last year
        public async Task<Dictionary<string, double>> GetMonthlyRevenueForPartnerLastYearAsync(int partnerID, double vatRate, double gracePeriodSeconds)
        {
            var startDate = DateTime.Today.AddYears(-1);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            var revenueByMonth = new Dictionary<string, double>();
            foreach (var session in sessions)
            {
                var key = $"{session.StartDate.Month}-{session.StartDate.Year}";
                var revenue = session.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds);

                if (revenueByMonth.ContainsKey(key))
                    revenueByMonth[key] += revenue;
                else
                    revenueByMonth[key] = revenue;
            }

            return revenueByMonth;
        }

        // Total charging sessions for a partner
        public async Task<double> GetTotalChargingSessionsForPartnerAsync(int partnerID)
        {
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            return sessions.Count;
        }

        // Total charging sessions for today for a partner
        public async Task<double> GetTotalChargingSessionsForPartnerTodayAsync(int partnerID)
        {
            var today = DateTime.Today;

            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null
                            && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID
                            && s.StartDate >= today
                            && s.StartDate < today.AddDays(1))
                .ToListAsync();

            return sessions.Count;
        }

        // Daily charging sessions for the last 30 days for a partner
        public async Task<Dictionary<DateTime, double>> GetDailyChargingSessionsForPartnerLast30DaysAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddDays(-30);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate)
                .ToListAsync();

            var sessionsByDay = new Dictionary<DateTime, double>();
            foreach (var session in sessions)
            {
                var date = session.StartDate.Date;

                if (sessionsByDay.ContainsKey(date))
                    sessionsByDay[date]++;
                else
                    sessionsByDay[date] = 1;
            }

            return sessionsByDay;
        }

        // Monthly charging sessions for the last year for a partner
        public async Task<Dictionary<string, double>> GetMonthlyChargingSessionsForPartnerLastYearAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddYears(-1);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            var sessionsByMonth = new Dictionary<string, double>();
            foreach (var session in sessions)
            {
                var key = $"{session.StartDate.Month}-{session.StartDate.Year}";

                if (sessionsByMonth.ContainsKey(key))
                    sessionsByMonth[key]++;
                else
                    sessionsByMonth[key] = 1;
            }

            return sessionsByMonth;
        }


        public async Task<double> GetTotalChargedMinutesForPartnerAsync(int partnerID)
        {
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            return sessions.Sum(s => s.ChargedMinutes ?? 0);
        }

        // Total charged minutes for today for a partner
        public async Task<double> GetTotalChargedMinutesForPartnerTodayAsync(int partnerID)
        {
            var today = DateTime.Today;

            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null
                            && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID
                            && s.StartDate >= today
                            && s.StartDate < today.AddDays(1))
                .ToListAsync();

            return sessions.Sum(s => s.ChargedMinutes ?? 0);
        }

        // Daily charged minutes for the last 30 days for a partner
        public async Task<Dictionary<DateTime, double>> GetDailyChargedMinutesForPartnerLast30DaysAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddDays(-30);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate)
                .ToListAsync();

            var chargedMinutesByDay = new Dictionary<DateTime, double>();
            foreach (var session in sessions)
            {
                var date = session.StartDate.Date;
                if (chargedMinutesByDay.ContainsKey(date))
                    chargedMinutesByDay[date] += session.ChargedMinutes ?? 0;
                else
                    chargedMinutesByDay[date] = session.ChargedMinutes ?? 0;
            }

            return chargedMinutesByDay;
        }

        // Monthly charged minutes for the last year for a partner
        public async Task<Dictionary<string, double>> GetMonthlyChargedMinutesForPartnerLastYearAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddYears(-1);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            var chargedMinutesByMonth = new Dictionary<string, double>();
            foreach (var session in sessions)
            {
                var key = $"{session.StartDate.Month}-{session.StartDate.Year}";

                if (chargedMinutesByMonth.ContainsKey(key))
                    chargedMinutesByMonth[key] += session.ChargedMinutes ?? 0;
                else
                    chargedMinutesByMonth[key] = session.ChargedMinutes ?? 0;
            }

            return chargedMinutesByMonth;
        }


        // Total energy consumed for a partner
        public async Task<double> GetTotalEnergyConsumedForPartnerAsync(int partnerID)
        {
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            return sessions.Sum(s => s.ChargedKwhs ?? 0);
        }

        // Total energy consumed for today for a partner
        public async Task<double> GetTotalEnergyConsumedForPartnerTodayAsync(int partnerID)
        {
            var today = DateTime.Today;

            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null
                            && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID
                            && s.StartDate >= today
                            && s.StartDate < today.AddDays(1))
                .ToListAsync();

            return sessions.Sum(s => s.ChargedKwhs ?? 0);
        }

        // Daily energy consumed for the last 30 days for a partner
        public async Task<Dictionary<DateTime, double>> GetDailyEnergyConsumedForPartnerLast30DaysAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddDays(-30);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate)
                .ToListAsync();

            var energyConsumedByDay = new Dictionary<DateTime, double>();
            foreach (var session in sessions)
            {
                var date = session.StartDate.Date;
                if (energyConsumedByDay.ContainsKey(date))
                    energyConsumedByDay[date] += session.ChargedKwhs ?? 0;
                else
                    energyConsumedByDay[date] = session.ChargedKwhs ?? 0;
            }

            return energyConsumedByDay;
        }

        // Monthly energy consumed for the last year for a partner
        public async Task<Dictionary<string, double>> GetMonthlyEnergyConsumedForPartnerLastYearAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddYears(-1);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate && s.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            var energyConsumedByMonth = new Dictionary<string, double>();
            foreach (var session in sessions)
            {
                var key = $"{session.StartDate.Month}-{session.StartDate.Year}";

                if (energyConsumedByMonth.ContainsKey(key))
                    energyConsumedByMonth[key] += session.ChargedKwhs ?? 0;
                else
                    energyConsumedByMonth[key] = session.ChargedKwhs ?? 0;
            }

            return energyConsumedByMonth;
        }
    }

}