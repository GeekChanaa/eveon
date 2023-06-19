using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public class CityRepository : Repository<City>,ICityRepository
    {
        public CityRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task<List<CityNameDto>> GetAllCityNamesByState(int stateID)
        {
            return await _context.Cities.Where(u => u.StateID == stateID).Select(u => new CityNameDto{Name = u.Name, ID = u.ID}).ToListAsync();
        }
    }
}

