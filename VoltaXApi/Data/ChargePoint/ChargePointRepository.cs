using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Dtos;
using AutoMapper;
using VoltaXApi.Helpers;
using AutoMapper.QueryableExtensions;
using Bogus.DataSets;
using VoltaXApi.Services;
using VoltaxApi.Dtos;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data
{
    public class ChargePointRepository : Repository<ChargePoint>, IChargePointRepository
    {
        private readonly IMapper _mapper;
        private readonly GlobalConfigurations _globalConfigurations;

        public ChargePointRepository(
            VoltaXApiDbContext context,
            IMapper mapper,
            GlobalConfigurations globalConfigurations) : base(context)
        {
            _mapper = mapper;
            _globalConfigurations = globalConfigurations;
        }

        public async Task<List<Connector>> GetChargePointConnectors(int chargePointID)
        {
            return await this._context.Connectors.Where(u => u.ChargePointID == chargePointID).ToListAsync();
        }

        // Charge Point Total Transactions Amount (total revenues from this charge point)
        public async Task<double> GetChargePointRevenue(string chargePointID, DateTime? startTime = null, DateTime? endTime = null)
        {
            var sessions = await _context.ChargingSessions
                .Where(s => s.EndDate != null && s.Connector.ChargePoint.ChargePointId == chargePointID
                        && (s.StartDate >= startTime || startTime == null)
                        && (s.EndDate <= endTime || endTime == null))
            .ToListAsync();

            double totalRevenue = 0;
            foreach (var session in sessions)
            {
                totalRevenue += session.TotalPriceWithVAT(_globalConfigurations.Vat, (int)_globalConfigurations.GracePeriod);
            }

            return totalRevenue;
        }

        // Charge Point Total Transactions Amount (total revenues from this charge point)
        public async Task<double> GetPartnerChargePointRevenue(int partnerID, string chargePointID)
        {
            var revenue = await _context.ChargePoints
                .Where(cp => cp.ChargingStation.PartnerID == partnerID &&
                            cp.ChargePointId == chargePointID &&
                            !cp.IsDeleted)
                .Select(cp => cp.Connectors.Sum(c => (c.PricePerKWh - c.CostPerKwh) * c.Power))
                .FirstOrDefaultAsync();

            return revenue;
        }

        override public async Task AddAsync(ChargePoint chargePoint)
        {
            chargePoint.ChargePointId = await this.GenerateChargePointId();
            chargePoint.QrValue = QRCodeService.GenerateQRCodeValueWithCustomUrl();
            await _context.Set<ChargePoint>().AddAsync(chargePoint);
            await _context.SaveChangesAsync();
        }
        
        public override async Task Update(ChargePoint chargePoint)
        {
            var entry = _context.Entry(chargePoint);
            if (entry.State == EntityState.Detached)
            {
                // Entities bound from request bodies never carry the password (it is not serialized): keep the stored hash.
                entry.State = EntityState.Modified;
                entry.Property(cp => cp.Password).IsModified = false;
            }
            else if (entry.State == EntityState.Unchanged)
            {
                entry.State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SetPasswordHash(int chargePointID, string passwordHash)
        {
            var cp = await _context.ChargePoints.FirstOrDefaultAsync(u => u.ID == chargePointID && !u.IsDeleted);
            if (cp == null) return false;
            cp.Password = passwordHash;
            cp.Username = cp.ChargePointId;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> GenerateChargePointId()
        {
            var latestChargePointNumber = await GetLatestChargePointNumberAsync();
            int nextNumber = latestChargePointNumber + 1;

            return $"VOLTAX-{nextNumber:D3}";
        }


        public async Task<ChargePointDisplayDto> GetChargePointByIdAsync(int id)
        {
            var chargePoint = await this._context.ChargePoints
                .Select(u => new ChargePointDisplayDto
                {
                    ID = u.ID,
                    ChargePointId = u.ChargePointId,
                    ChargingStationID = u.ChargingStationID,
                    ChargePointModelID = u.ChargePointModelID,
                    ChargePointBrandID = u.ChargePointBrandID,
                    ChargingStationName = u.ChargingStation.Name,
                    SerialNumber = u.SerialNumber,
                    ShowOnMap = u.ShowOnMap,
                    HasChargeCable = u.HasChargeCable,
                    Make = u.ChargePointBrand.Name,
                    ModelName = u.ChargePointModel.Name,
                    ModelImage = u.ChargePointModel.ImageUrl,
                    Status = u.Status,
                    Comment = u.Comment,
                    Username = u.Username,
                    HasPassword = u.Password != null && u.Password != "",
                    Latitude = u.ChargingStation.Latitude,
                    Longitude = u.ChargingStation.Longitude,
                    ClientCertThumb = u.ClientCertThumb,
                    Category = u.Category,
                })
                .FirstOrDefaultAsync(u => u.ID == id);
            return chargePoint;
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
                .Select(u => new ChargePointDisplayDto
                {
                    ID = u.ID,
                    ChargePointId = u.ChargePointId,
                    ChargingStationID = u.ChargingStationID,
                    ChargePointModelID = u.ChargePointModelID,
                    ChargePointBrandID = u.ChargePointBrandID,
                    ChargingStationName = u.ChargingStation.Name,
                    SerialNumber = u.SerialNumber,
                    ShowOnMap = u.ShowOnMap,
                    HasChargeCable = u.HasChargeCable,
                    Make = u.ChargePointBrand.Name,
                    ModelName = u.ChargePointModel.Name,
                    ModelImage = u.ChargePointModel.ImageUrl,
                    Status = u.Status,
                    Comment = u.Comment,
                    Username = u.Username,
                    HasPassword = u.Password != null && u.Password != "",
                    Address = u.ChargingStation.Address,
                    Latitude = u.ChargingStation.Latitude,
                    Longitude = u.ChargingStation.Longitude,
                    Country = u.ChargingStation.Country,
                    City = u.ChargingStation.City,
                    ClientCertThumb = u.ClientCertThumb,
                    Category = u.Category,
                }).AsQueryable();
           
            var chargePoint = await chargePointQueryable.FirstOrDefaultAsync(s => s.ID == chargePointID);

            return chargePoint;
        }

        public async Task SetShowOnMap(int chargepointID, bool val)
        {
            var cp = await _context.ChargePoints.FirstOrDefaultAsync(u => u.ID == chargepointID);
            cp.ShowOnMap = val;
            await this._context.SaveChangesAsync();
        }

        public async Task<bool> RegenerateQrValue(int chargePointID)
        {
            var cp = await _context.ChargePoints.FirstOrDefaultAsync(u => u.ID == chargePointID && !u.IsDeleted);
            if (cp == null) return false;
            cp.QrValue = QRCodeService.GenerateQRCodeValueWithCustomUrl();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> RegenerateAllQrValues()
        {
            var chargePoints = await _context.ChargePoints.Where(u => !u.IsDeleted).ToListAsync();
            foreach (var cp in chargePoints)
                cp.QrValue = QRCodeService.GenerateQRCodeValueWithCustomUrl();
            await _context.SaveChangesAsync();
            return chargePoints.Count;
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
                ChargingStationName = cp.ChargingStation.Name,
                SerialNumber = cp.SerialNumber,
                Category = cp.Category,
                Status = cp.Status,
                PartnerName = cp.ChargingStation.Partner.Name,
                IsConfigured = _context.ChargePointProvisionings.Any(p => p.ChargePointID == cp.ID
                    && (p.Method == ChargePointProvisioningMethodEnum.Automatic || p.Method == ChargePointProvisioningMethodEnum.Manual)),
                ConfigurationMethod = _context.ChargePointProvisionings.Where(p => p.ChargePointID == cp.ID)
                    .Select(p => (ChargePointProvisioningMethodEnum?)p.Method).FirstOrDefault(),
            });
            return chargePoints;
        }
        
        public IQueryable<ChargePointCRListDto> GetAllPartnerChargePoints(GlobalParams globalParams, int partnerID)
        {
            var chargePoints = GetAllAsync(globalParams).Where(u => u.ChargingStation.PartnerID == partnerID).Select(cp => new ChargePointCRListDto{
                ID = cp.ID,
                ChargePointId = cp.ChargePointId,
                ChargingStationName = cp.ChargingStation.Name,
                SerialNumber = cp.SerialNumber,
                Category = cp.Category,
                Status = cp.Status,
                PartnerName = cp.ChargingStation.Partner.Name,
                IsConfigured = _context.ChargePointProvisionings.Any(p => p.ChargePointID == cp.ID
                    && (p.Method == ChargePointProvisioningMethodEnum.Automatic || p.Method == ChargePointProvisioningMethodEnum.Manual)),
                ConfigurationMethod = _context.ChargePointProvisionings.Where(p => p.ChargePointID == cp.ID)
                    .Select(p => (ChargePointProvisioningMethodEnum?)p.Method).FirstOrDefault(),
            });
            return chargePoints;
        }

        public async Task<int> GetLatestChargePointNumberAsync()
        {

            var maxNumber = (await dbSet
                .Where(s => s.ChargePointId != null && s.ChargePointId.StartsWith("VOLTAX-"))
                .IgnoreQueryFilters()
                .Select(s => s.ChargePointId.Substring(7))
                .ToListAsync())
                .Where(numStr => int.TryParse(numStr, out _))
                .Select(numStr => int.Parse(numStr))
                .DefaultIfEmpty(0)
                .Max();

            return maxNumber;
        }

        public async Task<ChargePointDetailsForMobileDto?> GetChargePointByQrCode(string qrCode)
        {
            return await _context.ChargePoints
                .Where(cp => cp.QrValue != null && cp.QrValue.Contains(qrCode))
                .Select(cp => new ChargePointDetailsForMobileDto
                {
                    ID = cp.ID,
                    ChargePointId = cp.ChargePointId,
                    ChargingStationID = cp.ChargingStationID,
                    SerialNumber = cp.SerialNumber,
                    ShowOnMap = cp.ShowOnMap,
                    HasChargeCable = cp.HasChargeCable,
                    Status = cp.Status,
                    Comment = cp.Comment,
                    Address = cp.ChargingStation.Address,
                    Country = cp.ChargingStation.Country,
                    City = cp.ChargingStation.City,
                    Latitude = cp.ChargingStation.Latitude,
                    Longitude = cp.ChargingStation.Longitude,
                    Category = cp.Category,
                    ChargingPorts = cp.Connectors.Select(c => c.ConnectorType.ToString()).ToList(),
                    Connectors = cp.Connectors.Select(c => new ConnectorDto
                    {
                        ID = c.ID,
                        ConnectorID = c.ConnectorID,
                        EvseID = c.EvseID,
                        Name = c.ConnectorType.ToString(),
                        ConnectorType = c.ConnectorType,
                        PowerKw = c.MaxPower,
                        // Live status: latest ConnectorStatus row for this connector.
                        // No row => Disconnected (same convention as the dashboard counts).
                        Status = _context.ConnectorStatuses
                            .Where(s => s.ConnectorID == c.ID)
                            .OrderByDescending(s => s.LastStatusTime)
                            .Select(s => (ConnectorStatusEnumType?)s.LastStatus)
                            .FirstOrDefault() ?? ConnectorStatusEnumType.Disconnected
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}