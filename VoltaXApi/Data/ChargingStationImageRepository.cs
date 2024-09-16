using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class ChargingStationImageRepository : Repository<ChargingStationImage>,IChargingStationImageRepository
    {
      private readonly IMapper _mapper;
      public ChargingStationImageRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
      {   
        _mapper = mapper;
      }

      public async Task<List<ImageDto>> GetChargingStationImages(int chargingStationID)
      {
        var images = await this._context.ChargingStationImages.Where(u => u.ChargingStationID == chargingStationID).Include(u => u.Image).Select(cs => cs.Image).ToListAsync();
        var result = this._mapper.Map<List<Image>,List<ImageDto>>(images);
        return result;
      } 
    }
}

