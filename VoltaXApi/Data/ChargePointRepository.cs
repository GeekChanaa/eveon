using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
namespace VoltaXApi.Data
{
    public class ChargePointRepository : Repository<ChargePoint>, IChargePointRepository
    {
        public ChargePointRepository(VoltaXApiDbContext context) : base(context)
        {

        }

        public async Task<List<Connector>> GetChargePointConnectors(int chargePointID)
        {
            return await this._context.Connectors.Where(u => u.ChargePointID == chargePointID).ToListAsync();
        }

        // Charge Point Total Transactions Amount (total revenues from this charge point)
        public async Task<double> GetChargePointRevenue(string chargePointID, DateTime? start = null, DateTime? end = null)
        {
            var chargePoint = await this._context.ChargePoints.Include(u => u.Transactions).FirstOrDefaultAsync(u => u.ChargePointId == chargePointID);
            var transactions = chargePoint.Transactions.Where(t => (!start.HasValue || t.StartTime >= start.Value) && (!end.HasValue || t.StartTime <= end.Value));
            double total = 0;
            foreach (var transaction in transactions)
            {
                total += transaction.Amount;
            }

            return total;
        }


    }
}