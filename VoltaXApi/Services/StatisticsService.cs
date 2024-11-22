using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public class StatisticsService : IStatisticsService
  {
    private readonly IChargingSessionRepository _chargingSessionRepo;
    public StatisticsService(
      IChargingSessionRepository chargingSessionRepository
    )
    {
      _chargingSessionRepo = chargingSessionRepository;
    }

    // public async Task<ChargePointStatisticsDto> GetChargePointStatisticsDto(int chargePointID)
    // {
    //   int countChargingSessions = await _chargingSessionRepo.CountAsync(u => u.ChargePointID == chargePointID);
    // }
  }

}