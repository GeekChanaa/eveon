using System.Runtime.CompilerServices;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public interface IStatisticsService
  {
    Task<ChargePointStatisticsSummaryDto> GetChargePointStatisticsSummary(int chargePointID); 
    Task<double> GetTotalRevenueAsync(double vatRate, double gracePeriodSeconds);
    Task<double> GetTotalRevenueTodayAsync(double vatRate, double gracePeriodSeconds);
    Task<Dictionary<DateTime, double>> GetDailyRevenueLast30DaysAsync(double vatRate, double gracePeriodSeconds);
    Task<Dictionary<string, double>> GetMonthlyRevenueLastYearAsync(double vatRate, double gracePeriodSeconds);
  }
}