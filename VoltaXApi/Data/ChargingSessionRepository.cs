using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using AutoMapper;

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

        public async Task<List<ChargePointChargingSessionListDto>> GetChargePointChargingSessions(int chargePointID)
        {
            var chargingSessions = await _context.ChargingSessions.Include(u => u.User).Where(cs => cs.ChargePointID == chargePointID).ToListAsync();
            var result = _mapper.Map<List<ChargingSession>,List<ChargePointChargingSessionListDto>>(chargingSessions);
            return result;
        }

    }
}