using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Dtos;
using AutoMapper;
using VoltaXApi.Helpers;
using AutoMapper.QueryableExtensions;
using Bogus.DataSets;

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
            await _context.Set<ChargePoint>().AddAsync(chargePoint);
            await _context.SaveChangesAsync();
        }


        public async Task<ChargePointListDto> GetChargePointByIdAsync(int id)
        {
            var chargePoint= await this._context.ChargePoints
                .Include(u => u.Connectors)
                .Include(u => u.ChargingStation)
                .FirstOrDefaultAsync(u => u.ID == id);
            ChargePointListDto chargePointDto = _mapper.Map<ChargePointListDto>(chargePoint);
            return chargePointDto;
        }

        
        public async Task<List<ChargePointListDto>> GetChargingStationChargePoints(int chargingStationID)
        {
            var chargepoints = await this._context.ChargePoints.Where(cp => cp.ChargingStationID == chargingStationID).ToListAsync();
            List<ChargePointListDto> chargePointsDto = _mapper.Map<List<ChargePoint>, List<ChargePointListDto>>(chargepoints);
            return chargePointsDto;
        }

        public async Task<bool> IsChargePointIDUnique(string chargePointID)
        {
            return await this._context.ChargePoints.AnyAsync(cp => cp.ChargePointId == chargePointID);
        }

        public async Task<bool> IsChargePointSerialNumberUnique(string chargePointSerialNumber)
        {
            return await this._context.ChargePoints.AnyAsync(cp => cp.SerialNumber == chargePointSerialNumber);
        }

        public async Task<List<ChargePointSelectDto>> GetChargePointsIds()
        {
            return await this._context.ChargePoints.Select(u => new ChargePointSelectDto {
                ID = u.ID,
                ChargePointID = u.ChargePointId
            }).ToListAsync();
        }

        public async Task<ChargePoint> GetChargePointByChargePointIDAsync(string chargePointID)
        {
            return await _context.ChargePoints.FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointID);
        }

        public async Task<ChargePointDisplayDto> GetChargePointByID(int chargePointID, ChargePointIncludableHelper includableHelper)
        {
            var chargePointQueryable = _context.ChargePoints
                .Include(u => u.ChargingStation)
                .Include(u => u.ChargePointModel).AsQueryable();

            if(includableHelper.includeConnectors) 
                chargePointQueryable = chargePointQueryable.Include(s => s.Connectors);
           
            var chargePoint = await chargePointQueryable.FirstOrDefaultAsync(s => s.ID == chargePointID);

            var chargePointDto = _mapper.Map<ChargePoint, ChargePointDisplayDto>(chargePoint);
            return chargePointDto;
        }

        public async Task SetShowOnMap(int chargepointID, bool val)
        {
            var cp = await _context.ChargePoints.FirstOrDefaultAsync(u => u.ID == chargepointID);
            cp.ShowOnMap = val;
            await this._context.SaveChangesAsync();
        }

        public async Task SetHasChargeCable(int chargepointID, bool val)
        {
            var cp = await _context.ChargePoints.FirstOrDefaultAsync(u => u.ID == chargepointID);
            cp.HasChargeCable = val;
            await this._context.SaveChangesAsync();
        }

        public IQueryable<ChargePointCRListDto> GetAllChargePoints(GlobalParams globalParams)
        {
            var chargePoints = GetAllAsync(globalParams).Select(cp => new ChargePointCRListDto{
                ID = cp.ID,
                ChargePointId = cp.ChargePointId,
                Name = cp.Name,
                ChargingStationName = cp.ChargingStation.Name,
                SerialNumber = cp.SerialNumber,
                Category = cp.Category,
                Status = cp.Status,
                PartnerName = cp.ChargingStation.Partner.Name,
            });
            return chargePoints;
        }

        public async Task<int> GetLatestChargePointNumberAsync()
        {
            // Similar approach for charge point IDs
            var maxNumber = (await dbSet
                .Where(s => s.ChargePointId != null && s.ChargePointId.StartsWith("VOLTAX-"))
                .Select(s => s.ChargePointId.Substring(7))
                .ToListAsync())
                .Where(numStr => int.TryParse(numStr, out _))
                .Select(numStr => int.Parse(numStr))
                .DefaultIfEmpty(0)
                .Max();
                
            return maxNumber;
        }


    }
}