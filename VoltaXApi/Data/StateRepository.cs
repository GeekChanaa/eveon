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
    public class StateRepository : Repository<State>,IStateRepository
    {
        public StateRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task<List<StateNameDto>> GetAllStateNamesByCountry(int countryID)
        {
            return await _context.States.Where(u => u.CountryID == countryID).Select(u => new StateNameDto{Name = u.Name,ID = u.ID}).ToListAsync();
        }
    }
}

