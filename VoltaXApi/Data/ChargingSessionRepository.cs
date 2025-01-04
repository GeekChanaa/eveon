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
                    EndDate = cs.EndDate,
                    ConnectorID = cs.ConnectorID,
                    ConnectorRatio = cs.Connector.PricePerKWh,
                    Transactions = cs.Transactions.ToList(),
                    ConnectorCostRatio = cs.Connector.CostPerKwh
                })
                .FirstOrDefaultAsync();

            return result;
        }


    }
}