using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Mappers;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Icao;

namespace VoltaXApi.Data
{
    public class ChargingStationRepository : Repository<ChargingStation>, IChargingStationRepository
    {
        private readonly IMapper _mapper;
        private readonly VoltaXApi.Services.IBusinessClock _clock;
        private readonly GlobalConfigurations _globalConfigurations;
        public ChargingStationRepository(
            VoltaXApiDbContext context,
            IMapper mapper,
            VoltaXApi.Services.IBusinessClock clock,
            GlobalConfigurations globalConfigurations) : base(context)
        {
            _mapper = mapper;
            _clock = clock;
            _globalConfigurations = globalConfigurations;
        }

        private VoltaXApi.Services.SessionPricing Pricing => new(_globalConfigurations.Vat, _globalConfigurations.GracePeriod);

        public async Task<ChargingStationDisplayDto> GetChargingStationByIdAsync(int chargingStationID, ChargingStationIncludableHelper includableHelper)
        {
            var chargingStationQueryable = _context.ChargingStations.AsQueryable();

            if(includableHelper.includeChargePoints) 
                chargingStationQueryable = chargingStationQueryable.Include(s => s.ChargePoints);
            if(includableHelper.includeImages) 
                chargingStationQueryable = chargingStationQueryable.Include(s => s.ChargingStationImages).ThenInclude(s => s.Image);
           
            var chargingStation = await chargingStationQueryable.Select(cs => new ChargingStationDisplayDto{
                ID = cs.ID,
                Name = cs.Name,
                Address = cs.Address,
                Network = cs.Network,
                Category = cs.Category,
                ChargerQuantity = cs.ChargerQuantity,
                Country = cs.Country,
                State = cs.State,
                City = cs.City,
                Latitude = cs.Latitude,
                Longitude = cs.Longitude,
                Organisation = cs.Organisation,
                ParkingType = cs.ParkingType,
                Status = cs.Status,
                PartnerID = cs.PartnerID,
                PartnerName = cs.Partner.Name,
                WifiAmenity = cs.WifiAmenity,
                ParkingAmenity = cs.ParkingAmenity,
                RestaurantsAmenity = cs.RestaurantsAmenity,
                WashroomAmenity = cs.WashroomAmenity,
                SittingAreaAmenity = cs.SittingAreaAmenity,
            })
            .FirstOrDefaultAsync(s => s.ID == chargingStationID);

            
            return chargingStation;
        }

        public async Task<ChargingStationListDto> GetChargingStationByIdAsync(int chargingStationID)
        {
            var chargingStation = await this._context.ChargingStations
                .FirstOrDefaultAsync(u => u.ID == chargingStationID);
            ChargingStationListDto chargingStationDto = _mapper.Map<ChargingStationListDto>(chargingStation);

            return chargingStationDto;
        }

        /// <summary>Ended sessions on the station's (live) charge points.</summary>
        private IQueryable<ChargingSession> EndedSessionsAt(int chargingStationID) =>
            _context.ChargingSessions.AsNoTracking()
                .Where(s => s.EndDate != null && s.Connector!.ChargePoint!.ChargingStationID == chargingStationID);

        /// <summary>
        /// What drivers were billed (VAT included) for the station's ended sessions that started at or after
        /// <paramref name="start"/> and ended at or before <paramref name="end"/>.
        /// </summary>
        public async Task<double> GetChargingStationRevenue(int chargingStationID, DateTime? start = null, DateTime? end = null)
        {
            if (!await _context.ChargingStations.AnyAsync(s => s.ID == chargingStationID))
                throw new VoltaXApi.Exceptions.NotFoundException($"There is no charging station with the id {chargingStationID}");

            var sessions = EndedSessionsAt(chargingStationID);
            if (start != null)
                sessions = sessions.Where(s => s.StartDate >= start);
            if (end != null)
                sessions = sessions.Where(s => s.EndDate <= end);
            return (double)(await VoltaXApi.Services.SessionAggregation.TotalAsync(sessions, Pricing)).Revenue;
        }

        // Today first, then the days (or months) before it.
        public Task<IEnumerable<double>> GetChargingStationRevenueLast7Days(int chargingStationID) =>
            RevenuePerPeriod(chargingStationID, Enumerable.Range(0, 7)
                .Select(i => (_clock.StartOfDayUtc(_clock.Today.AddDays(-i)), _clock.StartOfDayUtc(_clock.Today.AddDays(1 - i)))));

        public Task<IEnumerable<double>> GetChargingStationRevenueLast30Days(int chargingStationID) =>
            RevenuePerPeriod(chargingStationID, Enumerable.Range(0, 30)
                .Select(i => (_clock.StartOfDayUtc(_clock.Today.AddDays(-i)), _clock.StartOfDayUtc(_clock.Today.AddDays(1 - i)))));

        public Task<IEnumerable<double>> GetChargingStationRevenueLast12Months(int chargingStationID) =>
            RevenuePerPeriod(chargingStationID, Enumerable.Range(0, 12).Select(i =>
            {
                var month = new DateTime(_clock.Today.Year, _clock.Today.Month, 1).AddMonths(-i);
                return (_clock.StartOfMonthUtc(month.Year, month.Month), _clock.StartOfDayUtc(month.AddMonths(1)));
            }));

        /// <summary>
        /// Revenue of the sessions that started and ended inside each period, read in one query (a session that runs
        /// past the end of its period counts in none, as with <see cref="GetChargingStationRevenue"/>).
        /// </summary>
        private async Task<IEnumerable<double>> RevenuePerPeriod(int chargingStationID, IEnumerable<(DateTime Start, DateTime End)> periods)
        {
            var list = periods.ToList();
            var from = list.Min(p => p.Start);
            var to = list.Max(p => p.End);
            if (!await _context.ChargingStations.AnyAsync(s => s.ID == chargingStationID))
                throw new VoltaXApi.Exceptions.NotFoundException($"There is no charging station with the id {chargingStationID}");

            var sessions = await VoltaXApi.Services.SessionAggregation.PricedAsync(
                EndedSessionsAt(chargingStationID).Where(s => s.StartDate >= from && s.EndDate <= to));
            var pricing = Pricing;
            return list.Select(p => sessions
                    .Where(s => s.StartDate >= p.Start && s.EndDate <= p.End)
                    .Sum(s => (double)s.TotalPriceWithVAT(pricing.VatRate, pricing.GracePeriodSeconds)))
                .ToList();
        }

        public async Task<IEnumerable<ChargingStationRevenue>> GetTop10ChargingStationsByRevenue(GlobalParams globalParams)
        {
            var stations = await GetAllAsync(globalParams).Select(s => new { s.ID, s.Name }).ToListAsync();
            var revenue = await VoltaXApi.Services.SessionAggregation.RevenueByStationAsync(
                _context.ChargingSessions.AsNoTracking().Where(s => s.EndDate != null), Pricing);

            // Stable sort: equal revenues keep the order of the station list.
            return stations
                .Select(s => new ChargingStationRevenue { ChargingStationID = s.ID, Revenue = revenue.GetValueOrDefault(s.ID) })
                .OrderByDescending(r => r.Revenue)
                .Take(10)
                .ToList();
        }

        public override async Task AddAsync(ChargingStation chargingStation)
        {
            // Get the latest ChargingStationID in the database
            var lastChargingStation = await _context.Set<ChargingStation>().OrderByDescending(c => c.Name).FirstOrDefaultAsync();

            int newNumber = 1;
            if (lastChargingStation != null)
            {
                // Extract the number from the ChargingStationID and increment it
                var lastNumber = int.Parse(lastChargingStation.Name.Substring(2));
                newNumber = lastNumber + 1;
            }

            // Generate new ChargingStationID
            chargingStation.Name = $"VX{newNumber.ToString("D4")}";

            // Add the new ChargingStation to the database
            await _context.Set<ChargingStation>().AddAsync(chargingStation);
            await _context.SaveChangesAsync();
        }



        // PARTNER CHARGING STATIONS FUNCTIONS
        public async Task<IEnumerable<ChargingStationRevenue>> GetPartnerTop10ChargingStationsByRevenue(int partnerID, GlobalParams globalParams)
        {
            var stations = await GetAllAsync(globalParams).Where(c => c.PartnerID == partnerID).Select(s => new { s.ID, s.Name }).ToListAsync();
            // The stations are the partner's: their revenue is what drivers were billed there.
            var revenue = await VoltaXApi.Services.SessionAggregation.RevenueByStationAsync(
                _context.ChargingSessions.AsNoTracking()
                    .Where(s => s.EndDate != null && s.Connector!.ChargePoint!.ChargingStation!.PartnerID == partnerID), Pricing);

            // Stable sort: equal revenues keep the order of the station list.
            return stations
                .Select(s => new ChargingStationRevenue { ChargingStationID = s.ID, Revenue = revenue.GetValueOrDefault(s.ID), ChargingStationName = s.Name })
                .OrderByDescending(r => r.Revenue)
                .Take(10)
                .ToList();
        }

        public async Task<ChargingStation> CreateChargingStation(ChargingStationCreateDto chargingStationCreateDto)
        {
            var chargingStation = _mapper.Map<ChargingStationCreateDto, ChargingStation>(chargingStationCreateDto);
            await this.AddAsync(chargingStation);
            return chargingStation;
        }

        public async Task<List<ChargingStationForMapDto>> GetChargingStationsForMap()
        {
            var chargingStations = await dbSet
                .Where(cs => cs.ChargePoints.Any(cp => cp.ShowOnMap == true))
                .Include(cs => cs.ChargingStationImages)
                    .ThenInclude(csi => csi.Image)
                .ToListAsync();
                
            var result = _mapper.Map<List<ChargingStation>,List<ChargingStationForMapDto>>(chargingStations);
            
            // Add the display image URL to each charging station
            foreach (var station in result)
            {
                var chargingStation = chargingStations.FirstOrDefault(cs => cs.ID == station.ID);
                if (chargingStation != null)
                {
                    var displayImage = chargingStation.ChargingStationImages
                        .Where(csi => csi.Image.Priority == 0)
                        .Select(csi => csi.Image.Url)
                        .FirstOrDefault();
                    station.ImageUrl = displayImage;
                }
            }
            
            return result;
        }

        public async Task<ChargingStationSearchResponseDto> SearchChargingStations(ChargingStationSearchParams searchParams)
        {
            var page = Math.Max(1, searchParams.Page);
            var pageSize = Math.Clamp(searchParams.PageSize, 1, 100);
            var types = searchParams.ConnectorTypes?.Distinct().ToList() ?? new List<ConnectorEnumType>();
            var availability = searchParams.Availability?.ToHashSet() ?? new HashSet<StationAvailability>();

            // Same visibility as the map: live stations with charge points shown on the map.
            var stations = _context.ChargingStations.AsNoTracking()
                .Where(s => !s.IsDeleted && s.ChargePoints.Any(cp => !cp.IsDeleted && cp.ShowOnMap == true));

            var words = (searchParams.Query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var word in words)
                stations = stations.Where(s => s.Address.Contains(word) || s.City.Contains(word) || s.Name.Contains(word)
                    || (s.State != null && s.State.Contains(word)) || (s.Country != null && s.Country.Contains(word)));

            if (types.Count > 0)
                stations = stations.Where(s => s.ChargePoints.Any(cp => !cp.IsDeleted && cp.ShowOnMap == true
                    && cp.Connectors.Any(c => !c.IsDeleted && c.ConnectorType != null && types.Contains(c.ConnectorType.Value))));

            var candidates = await stations
                .OrderBy(s => s.Name).ThenBy(s => s.ID)
                .Select(s => new
                {
                    s.ID, s.Name, s.Address, s.City, s.State, s.Country, s.Latitude, s.Longitude, s.Status,
                    ChargePoints = s.ChargePoints.Where(cp => !cp.IsDeleted && cp.ShowOnMap == true).Select(cp => new ChargePointSearchResultDto
                    {
                        ID = cp.ID,
                        ChargePointId = cp.ChargePointId,
                        Connectors = cp.Connectors
                            .Where(c => !c.IsDeleted && (types.Count == 0 || c.ConnectorType != null && types.Contains(c.ConnectorType.Value)))
                            .Select(c => new ConnectorSearchResultDto
                            {
                                ID = c.ID,
                                ConnectorID = c.ConnectorID,
                                EvseID = c.EvseID,
                                ConnectorType = c.ConnectorType,
                                PowerKw = c.Power,
                                // No status row => Disconnected (same convention as the dashboard counts).
                                Status = _context.ConnectorStatuses
                                    .Where(st => st.ConnectorID == c.ID && !st.IsDeleted)
                                    .OrderByDescending(st => st.LastStatusTime).ThenByDescending(st => st.ID)
                                    .Select(st => (ConnectorStatusEnumType?)st.LastStatus)
                                    .FirstOrDefault() ?? ConnectorStatusEnumType.Disconnected
                            }).ToList()
                    }).ToList()
                })
                .ToListAsync();

            // Availability depends on every connector's latest status, so it is filtered after loading.
            var results = candidates.Select(s =>
            {
                var connectors = s.ChargePoints.SelectMany(cp => cp.Connectors).ToList();
                var available = connectors.Count(c => c.Status == ConnectorStatusEnumType.Available);
                var occupied = connectors.Count(c => c.Status is ConnectorStatusEnumType.Occupied or ConnectorStatusEnumType.Reserved);
                // A station under maintenance or offline is unavailable whatever its connectors report.
                var outOfService = s.Status != ChargingStationStatusEnum.Available;
                return new ChargingStationSearchResultDto
                {
                    ID = s.ID, Name = s.Name, Address = s.Address, City = s.City, State = s.State, Country = s.Country,
                    Latitude = s.Latitude, Longitude = s.Longitude,
                    Availability = outOfService ? StationAvailability.Unavailable
                        : available > 0 ? StationAvailability.Available
                        : occupied > 0 ? StationAvailability.Occupied
                        : StationAvailability.Unavailable,
                    ConnectorTypes = connectors.Where(c => c.ConnectorType != null).Select(c => c.ConnectorType!.Value).Distinct().ToList(),
                    TotalConnectors = connectors.Count,
                    AvailableConnectors = outOfService ? 0 : available,
                    OccupiedConnectors = outOfService ? 0 : occupied,
                    UnavailableConnectors = outOfService ? connectors.Count : connectors.Count - available - occupied,
                    ChargePoints = s.ChargePoints
                };
            })
            .Where(s => availability.Count == 0 || availability.Contains(s.Availability))
            .ToList();

            return new ChargingStationSearchResponseDto
            {
                TotalCount = results.Count,
                Page = page,
                PageSize = pageSize,
                Items = results.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public async Task<List<ChargingStationSelectDto>> GetChargingStationNames(string searchTerm = "")
        {
            if(string.IsNullOrEmpty(searchTerm))
                return await this._context.ChargingStations
                    .Select(cs => new ChargingStationSelectDto {Name = cs.Name, ID = cs.ID}).ToListAsync();
            else
                return await this._context.ChargingStations.Where(cs => cs.Name.Contains(searchTerm))
                    .Select(cs => new ChargingStationSelectDto {Name = cs.Name, ID = cs.ID}).ToListAsync();

        }

        public async Task<bool> ChargingStationExistsByName(string name)
        {
            return await this._context.ChargingStations.AnyAsync(u => u.Name == name);
        }

        public async Task<List<ChargingStationListDto>> GetPartnerChargingStationsList(int partnerID)
        {
            return await this._context.ChargingStations
                .Where(cs => cs.PartnerID == partnerID)
                .Select(cs => new ChargingStationListDto{
                    ID = cs.ID,
                    Name = cs.Name
                })
                .ToListAsync();
        }

        public async Task<int> GetLatestStationNumberAsync()
        {
            var maxNumber = (await dbSet
                .Where(s => s.Name != null && s.Name.StartsWith("VCS-"))
                .Select(s => s.Name.Substring(4))
                .ToListAsync())
                .Where(numStr => int.TryParse(numStr, out _))
                .Select(numStr => int.Parse(numStr))
                .DefaultIfEmpty(0) 
                .Max();
                
            return maxNumber;
        }

        public async Task<List<ChargingStationForSelectDto>> GetChargingStationsForSelect()
        {
            return await dbSet.Select(cs => new ChargingStationForSelectDto{
                ID = cs.ID,
                Name = cs.Name
            }).ToListAsync();
        }



        
    }


}

