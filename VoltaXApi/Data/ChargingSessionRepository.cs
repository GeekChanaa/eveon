using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace VoltaXApi.Data
{
    public class ChargingSessionRepository : Repository<ChargingSession>, IChargingSessionRepository
    {
        private readonly IMapper _mapper;   
        public ChargingSessionRepository(
            VoltaXApiDbContext context,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public IQueryable<ChargePointChargingSessionListDto> GetChargePointChargingSessions(int chargePointID)
        {
            var chargingSessions = _context.ChargingSessions.Include(u => u.User).Include(u=> u.Connector)
                .Where(cs => cs.Connector.ChargePointID == chargePointID).AsQueryable().ProjectTo<ChargePointChargingSessionListDto>(_mapper.ConfigurationProvider);
            return chargingSessions;
        }

        public async Task<ChargingSession> GetLastChargingSession(int connectorID)
        {
            return await this._context.ChargingSessions
                    .Where(c => c.ConnectorID == connectorID && c.EndDate != null)
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefaultAsync();
        }

        public async Task<ChargingSessionInformationsDto> GetChargingSessionInformations(int chargingSessionID)        
        {
            var result = await _context.ChargingSessions
                .Where(cs => cs.ID == chargingSessionID)
                .Select(cs => new ChargingSessionInformationsDto
                {
                    UserName = cs.User.FirstName + " " + cs.User.LastName,
                    UserID = cs.User.ID,
                    CardID = cs.CardID,
                    Card = cs.Card,
                    ChargePointID = cs.Connector.ChargePoint.ID,
                    ChargePointName = cs.Connector.ChargePoint.ChargePointId,
                    TotalPrice = cs.Transactions.Sum(t => t.Amount),
                    KwhCharged = cs.Transactions.Sum(t => (t.MeterStop ?? 0) - t.MeterStart),
                    StartDate = cs.StartDate,
                    IdleTimeRatio = cs.Connector.PricePerIdleMinute,
                    EndDate = cs.EndDate,
                    ConnectorID = cs.ConnectorID,
                    ConnectorRatio = cs.Connector.PricePerKWh,
                    Transactions = cs.Transactions.ToList(),
                    ConnectorCostRatio = cs.Connector.CostPerKwh,
                })
                .FirstOrDefaultAsync();

            // Calculating Idle Time for charging session : 
            var transactionsTime = result.Transactions.Sum(t => (t.StopTime - t.StartTime).Value.TotalMinutes);
            result.ChargingTimeInMinutes = transactionsTime;
            // Get All connectorUptimes for these transactions
            var connectorUptimes = _context.ConnectorUptimes
                .Where(u => u.ConnectorUptimeStatus == ConnectorUptimeStatusEnum.Charging)
                .Where(u => result.Transactions.Select(u => u.ID).ToList().Contains(u.TransactionID ?? 0))
                .ToList();
            var totalConnectedTime = connectorUptimes.Sum(cu => (cu.EndDate - cu.StartDate).Value.TotalMinutes);
            Console.WriteLine("totalConnectedTime : " + totalConnectedTime);
            var idleTime = (int) (totalConnectedTime - transactionsTime);

            if(idleTime < 5)
                result.IdleMinutes = 0;
            else
                result.IdleMinutes = (decimal)idleTime;

            result.IdleTimePrice = result.IdleMinutes * result.IdleTimeRatio;

            result.TotalPrice += (double)result.IdleTimePrice + ((double)result.ConnectorRatio * result.KwhCharged);
            
            return result;  
        }


    }
}