using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Dtos;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class ChargePointRepository : Repository<ChargePoint>, IChargePointRepository
    {
        private readonly IMapper _mapper;
        public ChargePointRepository(VoltaXApiDbContext context, IMapper mapper ) : base(context)
        {
            _mapper = mapper;
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

        // Charge Point Total Transactions Amount (total revenues from this charge point)
        public async Task<double> GetPartnerChargePointRevenue(int partnerID ,string chargePointID, DateTime? start = null, DateTime? end = null)
        {
            var chargePoint = await this._context.ChargePoints.Where(u => u.ChargingStation.PartnerID == partnerID).Include(u => u.Transactions).FirstOrDefaultAsync(u => u.ChargePointId == chargePointID);
            var transactions = chargePoint.Transactions.Where(t => (!start.HasValue || t.StartTime >= start.Value) && (!end.HasValue || t.StartTime <= end.Value));
            double total = 0;
            foreach (var transaction in transactions)
            {
                total += transaction.Amount;
            }

            return total;
        }

        override public async Task AddAsync(ChargePoint chargePoint)
        {
            // Get the latest ChargePoint Name in the database
            var lastChargePoint = await _context.Set<ChargePoint>().OrderByDescending(c => c.Name).FirstOrDefaultAsync();

            int newNumber = 1;
            if (lastChargePoint != null)
            {
                // Extract the number from the Name and increment it
                var lastNumber = int.Parse(lastChargePoint.Name.Substring(3));  // Note the '3' here, because prefix 'VXC' has length 3
                newNumber = lastNumber + 1;
            }

            // Generate new ChargePoint Name
            chargePoint.Name = $"VXC{newNumber.ToString("D4")}";

            // Add the new ChargePoint to the database
            await _context.Set<ChargePoint>().AddAsync(chargePoint);
            await _context.SaveChangesAsync();
        }


        public async Task<ChargePointListDto> GetChargePointByIdAsync(int id)
        {
            var chargePoint= await this._context.ChargePoints.Include(u => u.Connectors).FirstOrDefaultAsync(u => u.ID == id);
            ChargePointListDto chargePointDto = _mapper.Map<ChargePointListDto>(chargePoint);
            return chargePointDto;
        }



    }
}