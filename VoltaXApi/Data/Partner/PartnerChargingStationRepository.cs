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
using AutoMapper.QueryableExtensions;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class PartnerChargingStationRepository : Repository<ChargingStation>, IPartnerChargingStationRepository
    {
        public PartnerChargingStationRepository(
            VoltaXApiDbContext context
            ) : base(context)
        {
        }

        public async Task<IEnumerable<ChargingStationRevenue>> GetPartnerTop10ChargingStationsByRevenue(int partnerID, GlobalParams globalParams)
        {
            var query = GetAllAsync(globalParams)
                .Where(cs => cs.PartnerID == partnerID && !cs.IsDeleted)
                .Select(cs => new ChargingStationRevenue
                {
                    ChargingStationID = cs.ID,
                    Revenue = cs.ChargePoints
                        .SelectMany(cp => cp.Connectors)
                        .Sum(c => (c.PricePerKWh - c.CostPerKwh) * c.Power)
                })
                .OrderByDescending(cs => cs.Revenue)
                .Take(10);

            return await query.ToListAsync();
        }

        
    }
}