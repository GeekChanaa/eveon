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
using AutoMapper;

namespace VoltaXApi.Data
{
    public class RatingRepository : Repository<Rating>,IRatingRepository
    {
      private readonly IMapper _mapper;

      public RatingRepository(
          VoltaXApiDbContext context,
          IMapper mapper) : base(context)
      {
            _mapper = mapper;
      }

      public async Task<List<RatingListDto>> GetChargePointRatings(int chargePointID)
      {
        var ratings = await this._context.Ratings.Where(u => u.Entity == "ChargePoint" && u.EntityID == chargePointID).Include(u => u.User).ToListAsync();
        var result = this._mapper.Map<List<Rating>, List<RatingListDto>>(ratings);
        return result;
      }
    }
}

