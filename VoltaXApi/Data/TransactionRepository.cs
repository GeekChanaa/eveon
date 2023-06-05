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
    public class TransactionRepository : Repository<Transaction>,ITransactionRepository
    {
        public TransactionRepository(VoltaXApiDbContext context) : base(context)
        {
            
        }

        public Task<double> CountEnergy(Expression<Func<Transaction, bool>> predicate)
        {
            var energySumTask = _context.Transactions
                .Where(predicate)
                .SumAsync(t => t.EnergyConsumed);

            return energySumTask;
        }
    }
}

