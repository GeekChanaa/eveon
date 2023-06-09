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
    public class CountryRepository : Repository<Country>,ICountryRepository
    {
        public CountryRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task<List<string>> GetAllCountryNames()
        {
            return await _context.Countries.Select(u => u.Name).ToListAsync();
        }
    }
}

