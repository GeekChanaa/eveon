using System.Runtime.CompilerServices;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public interface IStatisticsService
  {
    Task<ChargePointStatisticsSummaryDto> GetChargePointStatisticsSummary(int chargePointID); 
  }
}