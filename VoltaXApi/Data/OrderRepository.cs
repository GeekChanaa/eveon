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
    public class OrderRepository : Repository<Order>,IOrderRepository
    {
        public OrderRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public Task<decimal> CountRecharge(Expression<Func<Order, bool>> predicate)
        {
            var Amount = _context.Orders
                .Where(predicate)
                .SumAsync(t => t.Amount);

            return Amount;
        }
    }
}

