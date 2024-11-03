using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;

namespace VoltaXApi.Data
{
    public class ChargingSessionRepository : Repository<ChargingSession>, IChargingSessionRepository
    {
        public ChargingSessionRepository(VoltaXApiDbContext context) : base(context)
        {
        }
    }
}