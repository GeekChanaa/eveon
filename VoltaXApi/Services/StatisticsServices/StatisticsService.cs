using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IChargingSessionRepository _chargingSessionRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly VoltaXApiDbContext _context;
        public StatisticsService(
          IChargingSessionRepository chargingSessionRepository,
          ITransactionRepository transactionRepository,
          VoltaXApiDbContext context
        )
        {
            _chargingSessionRepo = chargingSessionRepository;
            _transactionRepo = transactionRepository;
            _context = context;
        }

        public async Task<ChargePointStatisticsSummaryDto> GetChargePointStatisticsSummary(int chargePointID)
        {
            var cpChargingSessions = await _chargingSessionRepo.GetChargePointChargingSessionsIDs(chargePointID);
            ChargePointStatisticsSummaryDto result = new ChargePointStatisticsSummaryDto
            {
                NbrChargingSessions = cpChargingSessions.Count,
                NbrChargingSessionsLastWeek = await _chargingSessionRepo.CountAsync(cs => cs.StartDate > DateTime.Now.AddDays(-7)),
                TotalEnergy = (await _transactionRepo.FindAsync(t => cpChargingSessions.Contains(t.ChargingSessionID))).Sum(t => t.MeterDifference) ?? 0,
                TotalEnergyLastWeek = (await _transactionRepo.FindAsync(t => t.StartTime > DateTime.Now.AddDays(-7) && cpChargingSessions.Contains(t.ChargingSessionID))).Sum(t => t.MeterDifference)
            };
            return result;
        }


        public async Task<double> GetTotalRevenueAsync(double vatRate, double gracePeriodSeconds)
        {
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null)
                .ToListAsync();

            double totalRevenue = 0;
            foreach (var session in sessions)
            {
                totalRevenue += session.TotalPriceWithVAT(vatRate, (int)gracePeriodSeconds);
            }

            return totalRevenue;
        }

        // Total revenue for today
        public async Task<double> GetTotalRevenueTodayAsync(double vatRate, double gracePeriodSeconds)
        {
            var today = DateTime.Today;

            var sessions = await _context.ChargingSessions
            .Where(s => s.EndDate != null
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
        public async Task<Dictionary<DateTime, double>> GetDailyRevenueLast30DaysAsync(double vatRate, double gracePeriodSeconds)
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
        public async Task<Dictionary<string, double>> GetMonthlyRevenueLastYearAsync(double vatRate, double gracePeriodSeconds)
        {
            var startDate = DateTime.Today.AddYears(-1);
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.StartDate >= startDate)
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