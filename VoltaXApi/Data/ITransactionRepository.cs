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
        Task<List<Transaction>> GetLatestTransactions(int nbrTransactions = 20);
        // Partner Functions
        Task<double> CountPartnerEnergy(int partnerID,Expression<Func<Transaction, bool>> predicate);
        Task<double> GetPartnerTotalEnergyConsumedAsync(int partnerID);
        Task<double> GetPartnerTotalEnergyConsumedTodayAsync(int partnerID);
        Task<Dictionary<DateTime, double>> GetPartnerDailyEnergyConsumedLast30DaysAsync(int partnerID);
        Task<Dictionary<string, double>> GetPartnerMonthlyEnergyConsumedLastYearAsync(int partnerID);
        Task<double> GetPartnerTotalEnergyConsumedBetween(int partnerID,DateTime date1, DateTime date2);
        Task<List<Transaction>> GetPartnerLatestTransactions(int partnerID,int nbrTransactions = 20);
        IQueryable<Transaction> GetCardTransactions(int cardID);
    }
}