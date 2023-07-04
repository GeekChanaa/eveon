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
using VoltaXApi.Mappers;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class ChargingStationRepository : Repository<ChargingStation>, IChargingStationRepository
    {
        private readonly IMapper _mapper;
        private readonly IChargePointRepository _chargePointRepo;
        public ChargingStationRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
            _chargePointRepo = new ChargePointRepository(context);
        }

        public async Task<ChargingStationListDto> GetChargingStationByIdAsync(int chargingStationID)
        {
            var chargingStation = await this._context.ChargingStations
                .Include(u => u.ChargePoints)
                    .ThenInclude(cp => cp.Connectors)
                .FirstOrDefaultAsync(u => u.ID == chargingStationID);
            ChargingStationListDto chargingStationDto = _mapper.Map<ChargingStationListDto>(chargingStation);

            return chargingStationDto;
        }

        // Getting charging station revenue
        public async Task<double> GetChargingStationRevenue(int chargingStationID, DateTime? start = null, DateTime? end = null)
        {
            var chargePoints = (await this._context.ChargingStations.Include(u => u.ChargePoints).FirstOrDefaultAsync(u => u.ID == chargingStationID)).ChargePoints;
            double total = 0;
            foreach (ChargePoint chargePoint in chargePoints)
            {
                total += await _chargePointRepo.GetChargePointRevenue(chargePoint.ChargePointId, start, end);
            }

            return total;
        }

        public async Task<IEnumerable<double>> GetChargingStationRevenueLast7Days(int chargingStationID)
        {
            List<double> revenueList = new List<double>();
            for (int i = 0; i < 7; i++)
            {
                DateTime start = DateTime.Today.AddDays(-i);
                DateTime end = start.AddDays(1);
                double revenue = await GetChargingStationRevenue(chargingStationID, start, end);
                revenueList.Add(revenue);
            }
            return revenueList;
        }

        public async Task<IEnumerable<double>> GetChargingStationRevenueLast30Days(int chargingStationID)
        {
            List<double> revenueList = new List<double>();
            for (int i = 0; i < 30; i++)
            {
                DateTime start = DateTime.Today.AddDays(-i);
                DateTime end = start.AddDays(1);
                double revenue = await GetChargingStationRevenue(chargingStationID, start, end);
                revenueList.Add(revenue);
            }
            return revenueList;
        }

        public async Task<IEnumerable<double>> GetChargingStationRevenueLast12Months(int chargingStationID)
        {
            List<double> revenueList = new List<double>();
            for (int i = 0; i < 12; i++)
            {
                DateTime start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-i);
                DateTime end = start.AddMonths(1);
                double revenue = await GetChargingStationRevenue(chargingStationID, start, end);
                revenueList.Add(revenue);
            }
            return revenueList;
        }


        public async Task<IEnumerable<ChargingStationRevenue>> GetTop10ChargingStationsByRevenue()
        {
            // Get all ChargingStations
            var chargingStations = await this._context.ChargingStations.Include(u => u.ChargePoints).ToListAsync();

            var revenues = new List<ChargingStationRevenue>();

            foreach (var cs in chargingStations)
            {
                var revenue = await GetChargingStationRevenue(cs.ID);
                revenues.Add(new ChargingStationRevenue { ChargingStationID = cs.ID, Revenue = revenue });
            }

            // Order by revenue and take top 10
            var top10Stations = revenues.OrderByDescending(r => r.Revenue).Take(10);

            return top10Stations;
        }

    }


}

