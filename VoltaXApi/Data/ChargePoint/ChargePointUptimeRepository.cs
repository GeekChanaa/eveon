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

        public async Task StartOnline(string chargePointID)
        {
            ChargePointUptime chargePointUT = new ChargePointUptime{
                ChargePointID = (await this._context.ChargePoints.FirstOrDefaultAsync(u => u.ChargePointId == chargePointID)).ID,
                StartDate = DateTime.UtcNow,
                EndDate = null,
                ChargePointUptimeStatus = ChargePointUptimeStatusEnum.Available
            };

            await _context.ChargePointUptimes.AddAsync(chargePointUT);
            await _context.SaveChangesAsync();
        }

        public async Task StopNormal(string chargePointID)
        {
            var cpID = (await this._context.ChargePoints.FirstOrDefaultAsync(u => u.ChargePointId == chargePointID)).ID;
            var chargePointUT = await this._context.ChargePointUptimes.FirstOrDefaultAsync(u => u.ChargePointID == cpID);
            chargePointUT.EndDate = DateTime.UtcNow;
            await this.Update(chargePointUT);
        }

    }
}