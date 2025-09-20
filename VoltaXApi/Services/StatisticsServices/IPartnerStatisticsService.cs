using System.Runtime.CompilerServices;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public interface IPartnerStatisticsService
  {
    Task<double> GetTotalRevenueForPartnerAsync(int partnerID, double vatRate, double gracePeriodSeconds);
    Task<double> GetTotalRevenueForPartnerTodayAsync(int partnerID, double vatRate, double gracePeriodSeconds);
    Task<Dictionary<DateTime, double>> GetDailyRevenueForPartnerLast30DaysAsync(int partnerID, double vatRate, double gracePeriodSeconds);
    Task<Dictionary<string, double>> GetMonthlyRevenueForPartnerLastYearAsync(int partnerID, double vatRate, double gracePeriodSeconds);

    Task<double> GetTotalChargingSessionsForPartnerAsync(int partnerID);
    Task<double> GetTotalChargingSessionsForPartnerTodayAsync(int partnerID);
    Task<Dictionary<DateTime, double>> GetDailyChargingSessionsForPartnerLast30DaysAsync(int partnerID);
    Task<Dictionary<string, double>> GetMonthlyChargingSessionsForPartnerLastYearAsync(int partnerID);

    Task<double> GetTotalChargedMinutesForPartnerAsync(int partnerID);
    Task<double> GetTotalChargedMinutesForPartnerTodayAsync(int partnerID);
    Task<Dictionary<DateTime, double>> GetDailyChargedMinutesForPartnerLast30DaysAsync(int partnerID);
    Task<Dictionary<string, double>> GetMonthlyChargedMinutesForPartnerLastYearAsync(int partnerID);

    Task<double> GetTotalEnergyConsumedForPartnerAsync(int partnerID);
    Task<double> GetTotalEnergyConsumedForPartnerTodayAsync(int partnerID);
    Task<Dictionary<DateTime, double>> GetDailyEnergyConsumedForPartnerLast30DaysAsync(int partnerID);
    Task<Dictionary<string, double>> GetMonthlyEnergyConsumedForPartnerLastYearAsync(int partnerID);
  }
}