using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<double> CountEnergy(Expression<Func<Transaction, bool>> predicate);
    }
}