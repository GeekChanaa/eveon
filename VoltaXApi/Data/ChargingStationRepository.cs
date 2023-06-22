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
        public ChargingStationRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<ChargingStationListDto> GetChargingStationByIdAsync(int chargingStationID)
        {
            var chargingStation =  await this._context.ChargingStations
                .Include(u => u.ChargePoints)
                    .ThenInclude(cp => cp.Connectors)
                .FirstOrDefaultAsync(u => u.ID == chargingStationID);
            ChargingStationListDto chargingStationDto = _mapper.Map<ChargingStationListDto>(chargingStation);

            return chargingStationDto;
        }
        
    }
    

}

