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
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(VoltaXApiDbContext context) : base(context)
        {

        }

        public Task<double> CountEnergy(Expression<Func<Transaction, bool>> predicate)
        {
            var energySumTask = _context.Transactions
                .Where(predicate)
                .SumAsync(t => t.MeterStart);

            return energySumTask;
        }

        public async Task<double> GetTotalEnergyConsumedAsync()
        {
            // Get all transactions where both MeterStart and MeterStop are not null
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null)
                .ToListAsync();

            // Calculate the total energy consumed
            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        public async Task<double> GetTotalEnergyConsumedTodayAsync()
        {
            var today = DateTime.Today;
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null && t.StartTime.Date == today)
                .ToListAsync();

            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        // Get Energy consumed between 2 dates
        public async Task<double> GetTotalEnergyConsumedBetween(DateTime dateStart , DateTime dateEnd)
        {
            var today = DateTime.Today;
            var transactions = await _context.Transactions
                                            .Where(t => t.MeterStart != null 
                                                    && t.MeterStop != null 
                                                    && t.StartTime >= dateStart 
                                                    && t.StartTime <= dateEnd)
                                            .ToListAsync();

            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        public async Task<Dictionary<DateTime, double>> GetDailyEnergyConsumedLast30DaysAsync()
        {
            var startDate = DateTime.Today.AddDays(-30);
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null && t.StartTime.Date >= startDate)
                .ToListAsync();

            var energyByDay = new Dictionary<DateTime, double>();
            foreach (var transaction in transactions)
            {
                var date = transaction.StartTime.Date;
                var energy = (double)(transaction.MeterStop - transaction.MeterStart);

                if (energyByDay.ContainsKey(date))
                {
                    energyByDay[date] += energy;
                }
                else
                {
                    energyByDay[date] = energy;
                }
            }

            return energyByDay;
        }


        public async Task<Dictionary<string, double>> GetMonthlyEnergyConsumedLastYearAsync()
        {
            var startDate = DateTime.Today.AddYears(-1);
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null && t.StartTime >= startDate)
                .ToListAsync();

            var energyByMonth = new Dictionary<string, double>();
            foreach (var transaction in transactions)
            {
                var monthYearKey = $"{transaction.StartTime.Month}-{transaction.StartTime.Year}";
                var energy = (double)(transaction.MeterStop - transaction.MeterStart);

                if (energyByMonth.ContainsKey(monthYearKey))
                {
                    energyByMonth[monthYearKey] += energy;
                }
                else
                {
                    energyByMonth[monthYearKey] = energy;
                }
            }

            return energyByMonth;
        }

        // Getting latest Transactions 
        public async Task<List<Transaction>> GetLatestTransactions(int nbrTransactions = 20)
        {
            return await  this._context.Transactions.OrderByDescending(u => u.StartTime).Take(nbrTransactions).ToListAsync();
        }


        public Task<double> CountPartnerEnergy(int partnerID,Expression<Func<Transaction, bool>> predicate)
        {
            var energySumTask = _context.Transactions
                .Where(predicate)
                .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                .SumAsync(t => t.MeterStart);

            return energySumTask;
        }

        public async Task<double> GetPartnerTotalEnergyConsumedAsync(int partnerID)
        {
            // Get all transactions where both MeterStart and MeterStop are not null
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null)
                .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            // Calculate the total energy consumed
            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        public async Task<double> GetPartnerTotalEnergyConsumedTodayAsync(int partnerID)
        {
            var today = DateTime.Today;
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null && t.StartTime.Date == today)
                .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        // Get Energy consumed between 2 dates
        public async Task<double> GetPartnerTotalEnergyConsumedBetween(int partnerID,DateTime dateStart , DateTime dateEnd)
        {
            var today = DateTime.Today;
            var transactions = await _context.Transactions
                                            .Where(t => t.MeterStart != null 
                                                    && t.MeterStop != null 
                                                    && t.StartTime >= dateStart 
                                                    && t.StartTime <= dateEnd)
                                            .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                                            .ToListAsync();

            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        public async Task<Dictionary<DateTime, double>> GetPartnerDailyEnergyConsumedLast30DaysAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddDays(-30);
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null && t.StartTime.Date >= startDate)
                .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            var energyByDay = new Dictionary<DateTime, double>();
            foreach (var transaction in transactions)
            {
                var date = transaction.StartTime.Date;
                var energy = (double)(transaction.MeterStop - transaction.MeterStart);

                if (energyByDay.ContainsKey(date))
                {
                    energyByDay[date] += energy;
                }
                else
                {
                    energyByDay[date] = energy;
                }
            }

            return energyByDay;
        }


        public async Task<Dictionary<string, double>> GetPartnerMonthlyEnergyConsumedLastYearAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddYears(-1);
            var transactions = await _context.Transactions
                .Where(t => t.MeterStart != null && t.MeterStop != null && t.StartTime >= startDate)
                .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            var energyByMonth = new Dictionary<string, double>();
            foreach (var transaction in transactions)
            {
                var monthYearKey = $"{transaction.StartTime.Month}-{transaction.StartTime.Year}";
                var energy = (double)(transaction.MeterStop - transaction.MeterStart);

                if (energyByMonth.ContainsKey(monthYearKey))
                {
                    energyByMonth[monthYearKey] += energy;
                }
                else
                {
                    energyByMonth[monthYearKey] = energy;
                }
            }

            return energyByMonth;
        }

        // Getting latest Transactions 
        public async Task<List<Transaction>> GetPartnerLatestTransactions(int partnerID,int nbrTransactions = 20)
        {
            return await  this._context.Transactions.Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID).OrderByDescending(u => u.StartTime).Take(nbrTransactions).ToListAsync();
        }



    }
}

