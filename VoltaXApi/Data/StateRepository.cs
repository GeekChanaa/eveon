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
    public class StateRepository : Repository<State>,IStateRepository
    {
        public StateRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task<List<string>> GetAllStateNamesByCountry(int countryID)
        {
            return await _context.States.Where(u => u.CountryID == countryID).Select(u => u.Name).ToListAsync();
        }
    }
}

