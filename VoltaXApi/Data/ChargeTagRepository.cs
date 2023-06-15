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
    public class ChargeTagRepository : Repository<ChargeTag>, IChargeTagRepository
    {
        public ChargeTagRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public async Task<ChargeTag?> GetByTagId(string idTag)
        {
            return await _context.ChargeTags.Where(u => u.TagID == idTag).FirstOrDefaultAsync();
        }
        
    }
    

}

