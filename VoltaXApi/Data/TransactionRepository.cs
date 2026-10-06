using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.Data
{
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        private readonly IMapper _mapper;
        private readonly VoltaXApi.Services.IBusinessClock _clock;

        public TransactionRepository(VoltaXApiDbContext context, IMapper mapper, VoltaXApi.Services.IBusinessClock clock)
            : base(context)
        {
            _mapper = mapper;
            _clock = clock;
        }

        public Task<double> CountEnergy(Expression<Func<Transaction, bool>> predicate)
        {
            var energySumTask = _context.Transactions.Where(predicate).SumAsync(t => t.MeterStart);

            return energySumTask;
        }

        public Task<double> GetTotalEnergyConsumedAsync() =>
            SumEnergy(Completed());

        public Task<double> GetTotalEnergyConsumedTodayAsync() =>
            SumEnergy(Today(Completed()));

        // Get Energy consumed between 2 dates
        public Task<double> GetTotalEnergyConsumedBetween(DateTime dateStart, DateTime dateEnd) =>
            SumEnergy(Completed().Where(t => t.StartTime >= dateStart && t.StartTime <= dateEnd));

        public async Task<Dictionary<DateTime, double>> GetDailyEnergyConsumedLast30DaysAsync() =>
            EnergyByDay(await LoadEnergy(Last30Days(Completed())));

        public async Task<Dictionary<string, double>> GetMonthlyEnergyConsumedLastYearAsync() =>
            EnergyByMonth(await LoadEnergy(LastYear(Completed())));

        // Getting latest Transactions
        public async Task<List<Transaction>> GetLatestTransactions(int nbrTransactions = 20)
        {
            return await this
                ._context.Transactions.OrderByDescending(u => u.StartTime)
                .Take(nbrTransactions)
                .ToListAsync();
        }

        public Task<double> CountPartnerEnergy(
            int partnerID,
            Expression<Func<Transaction, bool>> predicate
        )
        {
            var energySumTask = _context
                .Transactions.Where(predicate)
                .Where(u => u.Connector.ChargePoint.ChargingStation.PartnerID == partnerID)
                .SumAsync(t => t.MeterStart);

            return energySumTask;
        }

        public Task<double> GetPartnerTotalEnergyConsumedAsync(int partnerID) =>
            SumEnergy(Partner(Completed(), partnerID));

        public Task<double> GetPartnerTotalEnergyConsumedTodayAsync(int partnerID) =>
            SumEnergy(Today(Partner(Completed(), partnerID)));

        // Get Energy consumed between 2 dates
        public Task<double> GetPartnerTotalEnergyConsumedBetween(int partnerID, DateTime dateStart, DateTime dateEnd) =>
            SumEnergy(Partner(Completed(), partnerID).Where(t => t.StartTime >= dateStart && t.StartTime <= dateEnd));

        public async Task<Dictionary<DateTime, double>> GetPartnerDailyEnergyConsumedLast30DaysAsync(int partnerID) =>
            EnergyByDay(await LoadEnergy(Last30Days(Partner(Completed(), partnerID))));

        public async Task<Dictionary<string, double>> GetPartnerMonthlyEnergyConsumedLastYearAsync(int partnerID) =>
            EnergyByMonth(await LoadEnergy(LastYear(Partner(Completed(), partnerID))));

        #region Energy statistics

        // Transactions with a stop meter value; "today", daily and monthly use business-time-zone days.
        private IQueryable<Transaction> Completed() =>
            _context.Transactions.AsNoTracking().Where(t => t.MeterStop != null);

        private static IQueryable<Transaction> Partner(IQueryable<Transaction> query, int partnerID) =>
            query.Where(t => t.Connector!.ChargePoint!.ChargingStation!.PartnerID == partnerID);

        private IQueryable<Transaction> Today(IQueryable<Transaction> query)
        {
            var from = _clock.StartOfDayUtc(_clock.Today);
            var to = _clock.StartOfDayUtc(_clock.Today.AddDays(1));
            return query.Where(t => t.StartTime >= from && t.StartTime < to);
        }

        private IQueryable<Transaction> Last30Days(IQueryable<Transaction> query)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddDays(-30));
            return query.Where(t => t.StartTime >= from);
        }

        private IQueryable<Transaction> LastYear(IQueryable<Transaction> query)
        {
            var from = _clock.StartOfDayUtc(_clock.Today.AddYears(-1));
            return query.Where(t => t.StartTime >= from);
        }

        private static async Task<double> SumEnergy(IQueryable<Transaction> query) =>
            await query.SumAsync(t => t.MeterStop - t.MeterStart) ?? 0;

        private static async Task<List<(DateTime StartTime, double Energy)>> LoadEnergy(IQueryable<Transaction> query)
        {
            var rows = await query.Select(t => new { t.StartTime, Energy = t.MeterStop!.Value - t.MeterStart }).ToListAsync();
            return rows.Select(r => (r.StartTime, r.Energy)).ToList();
        }

        private Dictionary<DateTime, double> EnergyByDay(IEnumerable<(DateTime StartTime, double Energy)> rows) =>
            rows.GroupBy(r => _clock.ToLocal(r.StartTime).Date).ToDictionary(g => g.Key, g => g.Sum(r => r.Energy));

        private Dictionary<string, double> EnergyByMonth(IEnumerable<(DateTime StartTime, double Energy)> rows) =>
            rows.GroupBy(r =>
            {
                var local = _clock.ToLocal(r.StartTime);
                return $"{local.Month}-{local.Year}";
            }).ToDictionary(g => g.Key, g => g.Sum(r => r.Energy));

        #endregion

        // Getting latest Transactions
        public async Task<List<Transaction>> GetPartnerLatestTransactions(
            int partnerID,
            int nbrTransactions = 20
        )
        {
            return await this
                ._context.Transactions.Where(u =>
                    u.Connector.ChargePoint.ChargingStation.PartnerID == partnerID
                )
                .OrderByDescending(u => u.StartTime)
                .Take(nbrTransactions)
                .ToListAsync();
        }

        public IQueryable<Transaction> GetCardTransactions(int cardID)
        {
            return _context.Transactions.Where(t => t.StartCardID == cardID || t.StopCardID == cardID);
        }

        public IQueryable<TransactionListDto> GetChargePointTransactions(int chargePointID, GlobalParams globalParams)
        {
            var transactions = GetAllAsync(globalParams).Where(u => u.Connector.ChargePointID == chargePointID)
                .OrderByDescending(u => u.StartTime);

            var transactionsDto = _mapper.ProjectTo<TransactionListDto>(transactions);
            return transactionsDto;
        }

        public IQueryable<TransactionListDto> GetAllTransactions(GlobalParams globalParams)
        {
            var transactions = GetAllAsync(globalParams).Select(u => new TransactionListDto{
                Uid = u.Uid,
                ChargePointID = u.Connector.ChargePoint.ChargePointId,
                ConnectorID = u.ConnectorID ?? 0,
                ConnectorName = u.Connector.ConnectorName,
                ChargingSessionID = u.ChargingSessionID,
                StartTagId = u.StartCard.CardNumber,
                StartTime = u.StartTime,
                MeterStart = u.MeterStart,
                StartResult = u.StartResult,
                StopTagId = u.StopCard.CardNumber,
                StopTime = u.StopTime,
                MeterStop = u.MeterStop,
                StopReason = u.StopReason,
                Status = u.Status,
                Amount = u.Amount,
            })
                .OrderByDescending(u => u.StartTime);
            return transactions;
        }

        public async Task<TransactionListDto> GetTransactionByID(string transactionID)
        {
            return await dbSet.Select(u => new TransactionListDto{
                Uid = u.Uid,
                ChargePointID = u.Connector.ChargePoint.ChargePointId,
                ConnectorID = u.ConnectorID ?? 0,
                ConnectorName = u.Connector.ConnectorName,
                ChargingSessionID = u.ChargingSessionID,
                StartTagId = u.StartCard.CardNumber,
                StartTime = u.StartTime,
                MeterStart = u.MeterStart,
                StartResult = u.StartResult,
                StopTagId = u.StopCard.CardNumber,
                StopTime = u.StopTime,
                MeterStop = u.MeterStop,
                StopReason = u.StopReason,
                Status = u.Status,
                Amount = u.Amount,
            })
                .FirstOrDefaultAsync(u => u.Uid == transactionID);
        }



        
    }
}
