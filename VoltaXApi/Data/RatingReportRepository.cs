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
    public class RatingReportRepository : Repository<RatingReport>,IRatingReportRepository
    {
        public RatingReportRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }
    }
}

