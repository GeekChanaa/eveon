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
        Task<double> GetTotalEnergyConsumedAsync();
        Task<double> GetTotalEnergyConsumedTodayAsync();
        Task<Dictionary<DateTime, double>> GetDailyEnergyConsumedLast30DaysAsync();
        Task<Dictionary<string, double>> GetMonthlyEnergyConsumedLastYearAsync();
        Task<double> GetTotalEnergyConsumedBetween(DateTime date1, DateTime date2);
    }
}