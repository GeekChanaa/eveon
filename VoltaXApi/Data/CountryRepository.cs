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

namespace VoltaXApi.Data
{
    public class CountryRepository : Repository<Country>,ICountryRepository
    {
        public CountryRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task<List<CountryNameDto>> GetAllCountryNames()
        {
            return await _context.Countries.Select(u => new CountryNameDto{Name = u.Name, ID = u.ID}).ToListAsync();
        }
    }
}