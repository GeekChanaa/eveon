using System.Runtime.CompilerServices;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public interface IStatisticsService
  {
    Task<ChargePointStatisticsSummaryDto> GetChargePointStatisticsSummary(int chargePointID); 
    Task<double> GetTotalRevenueAsync(decimal vatRate, double gracePeriodSeconds);
    Task<double> GetTotalRevenueTodayAsync(decimal vatRate, double gracePeriodSeconds);
    Task<Dictionary<DateTime, double>> GetDailyRevenueLast30DaysAsync(decimal vatRate, double gracePeriodSeconds);
    Task<Dictionary<string, double>> GetMonthlyRevenueLastYearAsync(decimal vatRate, double gracePeriodSeconds);
  }
}