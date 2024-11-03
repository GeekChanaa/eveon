using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;

namespace VoltaXApi.Data
{
    public class ChargePointUptimeRepository : Repository<ChargePointUptime>, IChargePointUptimeRepository
    {
        public ChargePointUptimeRepository(VoltaXApiDbContext context) : base(context)
        {
        }
    }
}