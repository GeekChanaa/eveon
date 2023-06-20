using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
namespace VoltaXApi.Data
{
    public class ChargePointRepository : Repository<ChargePoint> , IChargePointRepository
    {
        public ChargePointRepository(VoltaXApiDbContext context) : base(context)
        {

        }

        public async Task<List<Connector>> GetChargePointConnectors(int chargePointID)
        {
            return await this._context.Connectors.Where(u => u.ChargePointID == chargePointID).ToListAsync();
        }

    }
}