using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<double> CountRecharge(Expression<Func<Order, bool>> predicate);
    }
}