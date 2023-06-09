using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public class CityRepository : Repository<City>,ICityRepository
    {
        public CityRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task<List<string>> GetAllCityNamesByCountry(int countryID)
        {
            return await _context.Cities.Where(u => u.CountryID == countryID).Select(u => u.Name).ToListAsync();
        }
    }
}

