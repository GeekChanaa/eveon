using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using System.Data.Entity;

namespace VoltaXApi.Services
{
  public class StatisticsService : IStatisticsService
  {
    private readonly IChargingSessionRepository _chargingSessionRepo;
    private readonly ITransactionRepository _transactionRepo;
    public StatisticsService(
      IChargingSessionRepository chargingSessionRepository,
      ITransactionRepository transactionRepository
    )
    {
      _chargingSessionRepo = chargingSessionRepository;
      _transactionRepo = transactionRepository;
    }

    public async Task<ChargePointStatisticsSummaryDto> GetChargePointStatisticsSummary(int chargePointID)
    {
      var cpChargingSessions = await _chargingSessionRepo.GetChargePointChargingSessionsIDs(chargePointID);
      ChargePointStatisticsSummaryDto result = new ChargePointStatisticsSummaryDto{
        NbrChargingSessions = cpChargingSessions.Count,
        NbrChargingSessionsLastWeek = await _chargingSessionRepo.CountAsync(cs => cs.StartDate > DateTime.Now.AddDays(-7)),
        TotalEnergy = (await _transactionRepo.FindAsync(t => cpChargingSessions.Contains(t.ChargingSessionID))).Sum(t => t.MeterDifference) ?? 0,
        TotalEnergyLastWeek = (await _transactionRepo.FindAsync(t => t.StartTime > DateTime.Now.AddDays(-7) && cpChargingSessions.Contains(t.ChargingSessionID))).Sum(t => t.MeterDifference)
      };
      return result;
    }
  }

}