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

        public TransactionRepository(VoltaXApiDbContext context, IMapper mapper)
            : base(context)
        {
            _mapper = mapper;
        }

        public Task<double> CountEnergy(Expression<Func<Transaction, bool>> predicate)
        {
            var energySumTask = _context.Transactions.Where(predicate).SumAsync(t => t.MeterStart);

            return energySumTask;
        }

        public async Task<double> GetTotalEnergyConsumedAsync()
        {
            // Get all transactions where both MeterStart and MeterStop are not null
            var transactions = await _context
                .Transactions.Where(t => t.MeterStart != null && t.MeterStop != null)
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
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null && t.MeterStop != null && t.StartTime.Date == today
                )
                .ToListAsync();

            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        // Get Energy consumed between 2 dates
        public async Task<double> GetTotalEnergyConsumedBetween(
            DateTime dateStart,
            DateTime dateEnd
        )
        {
            var today = DateTime.Today;
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null
                    && t.MeterStop != null
                    && t.StartTime >= dateStart
                    && t.StartTime <= dateEnd
                )
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
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null && t.MeterStop != null && t.StartTime.Date >= startDate
                )
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
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null && t.MeterStop != null && t.StartTime >= startDate
                )
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
                .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                .SumAsync(t => t.MeterStart);

            return energySumTask;
        }

        public async Task<double> GetPartnerTotalEnergyConsumedAsync(int partnerID)
        {
            // Get all transactions where both MeterStart and MeterStop are not null
            var transactions = await _context
                .Transactions.Where(t => t.MeterStart != null && t.MeterStop != null)
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
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null && t.MeterStop != null && t.StartTime.Date == today
                )
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
        public async Task<double> GetPartnerTotalEnergyConsumedBetween(
            int partnerID,
            DateTime dateStart,
            DateTime dateEnd
        )
        {
            var today = DateTime.Today;
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null
                    && t.MeterStop != null
                    && t.StartTime >= dateStart
                    && t.StartTime <= dateEnd
                )
                .Where(u => u.ChargePoint.ChargingStation.PartnerID == partnerID)
                .ToListAsync();

            double totalEnergy = 0;
            foreach (var transaction in transactions)
            {
                totalEnergy += (double)(transaction.MeterStop - transaction.MeterStart);
            }

            return totalEnergy;
        }

        public async Task<
            Dictionary<DateTime, double>
        > GetPartnerDailyEnergyConsumedLast30DaysAsync(int partnerID)
        {
            var startDate = DateTime.Today.AddDays(-30);
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null && t.MeterStop != null && t.StartTime.Date >= startDate
                )
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

        public async Task<Dictionary<string, double>> GetPartnerMonthlyEnergyConsumedLastYearAsync(
            int partnerID
        )
        {
            var startDate = DateTime.Today.AddYears(-1);
            var transactions = await _context
                .Transactions.Where(t =>
                    t.MeterStart != null && t.MeterStop != null && t.StartTime >= startDate
                )
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
        public async Task<List<Transaction>> GetPartnerLatestTransactions(
            int partnerID,
            int nbrTransactions = 20
        )
        {
            return await this
                ._context.Transactions.Where(u =>
                    u.ChargePoint.ChargingStation.PartnerID == partnerID
                )
                .OrderByDescending(u => u.StartTime)
                .Take(nbrTransactions)
                .ToListAsync();
        }

        public IQueryable<Transaction> GetCardTransactions(int cardID)
        {
            return _context.Transactions.Where(t => t.CardID == cardID);
        }

        public async Task StartTransaction(
            TransactionEventRequest transactionEventRequest,
            TransactionEventResponse transactionEventResponse,
            ChargePointStatus chargePointStatus,
            int connectorID,
            string? idTag,
            string errorCode,
            double meterKWH
        )
        {
            try
            {
                transactionEventResponse.IdTokenInfo.Status = await ValidateCard(idTag);

                if (transactionEventResponse.IdTokenInfo.Status== AuthorizationStatusEnumType.Accepted)
                {
                    try
                    {
                        Transaction transaction = new Transaction();
                        transaction.Uid = transactionEventRequest.TransactionInfo.TransactionId;
                        transaction.ChargePointID = chargePointStatus.Id;
                        transaction.ConnectorID = connectorID;
                        transaction.StartTagId = idTag;
                        transaction.StartTime = DateTime.Parse(transactionEventRequest.Timestamp);
                        transaction.MeterStart = meterKWH;
                        transaction.StartResult = transactionEventRequest.TriggerReason.ToString();
                        _context.Add<Transaction>(transaction);

                        _context.SaveChanges();
                    }
                    catch (Exception exp)
                    {
                        errorCode = ErrorCodes.InternalError;
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("StartTransaction => Exception: {0}", exp.Message);
                Console.WriteLine(exp.StackTrace);
                transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
            }
        }

        public async Task UpdateTransaction(
            TransactionEventRequest transactionEventRequest,
            TransactionEventResponse transactionEventResponse,
            ChargePointStatus chargePointStatus,
            int connectorID,
            string? idTag,
            string errorCode,
            double meterKWH
        )
        {
            try
            {
                Transaction? transaction = _context
                    .Transactions.Where(t =>
                        t.Uid == transactionEventRequest.TransactionInfo.TransactionId
                    )
                    .OrderByDescending(t => t.ID)
                    .FirstOrDefault();
                if (
                    transaction == null
                    || transaction.ChargePointID != chargePointStatus.Id
                    || transaction.StopTime.HasValue
                )
                {
                    // unknown transaction id or already stopped transaction
                    // => find latest transaction for the charge point and check if its open
                    Console.WriteLine(
                        "UpdateTransaction => Unknown or closed transaction uid={0}",
                        transactionEventRequest.TransactionInfo?.TransactionId
                    );
                    // find latest transaction for this charge point
                    transaction = _context
                        .Transactions.Where(t =>
                            t.ChargePointID == chargePointStatus.Id && t.ConnectorID == connectorID
                        )
                        .OrderByDescending(t => t.ID)
                        .FirstOrDefault();

                    if (transaction != null)
                    {
                        Console.WriteLine(
                            "UpdateTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'",
                            transaction.ID,
                            transaction.StartTime.ToString("O"),
                            transaction?.StopTime?.ToString("O")
                        );
                        if (transaction.StopTime.HasValue)
                        {
                            Console.WriteLine(
                                "UpdateTransaction => Last transaction (id={0}) is already closed ",
                                transaction.ID
                            );
                            transaction = null;
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            "UpdateTransaction => Found no transaction for charge point '{0}' and connectorID '{1}'",
                            chargePointStatus.Id,
                            connectorID
                        );
                    }
                }

                if (transaction != null)
                {
                    if (meterKWH >= 0)
                    {
                        transaction.MeterStop = meterKWH;
                        _context.SaveChanges();
                    }
                }
                else
                {
                    errorCode = ErrorCodes.PropertyConstraintViolation;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("UpdateTransaction => Exception: {0}", exp.Message);
                transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
            }
        }

        public async Task EndTransaction(
            TransactionEventRequest transactionEventRequest,
            TransactionEventResponse transactionEventResponse,
            ChargePointStatus chargePointStatus,
            int connectorID,
            string? idTag,
            string errorCode,
            double meterKWH
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idTag))
                    transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                else
                    transactionEventResponse.IdTokenInfo.Status = await ValidateCard(idTag);

                Transaction? transaction = _context
                    .Transactions.Where(t =>
                        t.Uid == transactionEventRequest.TransactionInfo.TransactionId
                    )
                    .OrderByDescending(t => t.ID)
                    .FirstOrDefault();
                if (
                    transaction == null
                    || transaction.ChargePointID != chargePointStatus.Id
                    || transaction.StopTime.HasValue
                )
                {
                    // unknown transaction id or already stopped transaction
                    // => find latest transaction for the charge point and check if its open
                    Console.WriteLine(
                        "EndTransaction => Unknown or closed transaction uid={0}",
                        transactionEventRequest.TransactionInfo?.TransactionId
                    );
                    // find latest transaction for this charge point
                    transaction = _context
                        .Transactions.Where(t =>
                            t.ChargePointID == chargePointStatus.Id && t.ConnectorID == connectorID
                        )
                        .OrderByDescending(t => t.ID)
                        .FirstOrDefault();

                    if (transaction != null)
                    {
                        Console.WriteLine(
                            "EndTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'",
                            transaction.ID,
                            transaction.StartTime.ToString("O"),
                            transaction?.StopTime?.ToString("O")
                        );
                        if (transaction.StopTime.HasValue)
                        {
                            Console.WriteLine(
                                "EndTransaction => Last transaction (id={0}) is already closed ",
                                transaction.ID
                            );
                            transaction = null;
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            "EndTransaction => Found no transaction for charge point '{0}' and connectorID '{1}'",
                            chargePointStatus.Id,
                            connectorID
                        );
                    }
                }

                if (transaction != null)
                {
                    // check current tag against start tag
                    // bool valid = true;
                    // if (!string.Equals(transaction.StartTagId, idTag, StringComparison.InvariantCultureIgnoreCase))
                    // {
                    //     // tags are different => same group?
                    //     ChargeTag? startTag = _context.ChargeTags.Where(c => c.TagID == transaction.StartTagId).FirstOrDefault();
                    //     if (startTag != null)
                    //     {
                    //         if (!string.Equals(startTag.ParentTagId, ct?.ParentTagId, StringComparison.InvariantCultureIgnoreCase))
                    //         {
                    //             Console.WriteLine("EndTransaction => Start-Tag ('{0}') and End-Tag ('{1}') do not match: Invalid!", transaction.StartTagId, ct?.ID);
                    //             transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    //             valid = false;
                    //         }
                    //         else
                    //         {
                    //             Console.WriteLine("EndTransaction => Different charge tags but matching group ('{0}')", ct?.ParentTagId);
                    //         }
                    //     }
                    //     else
                    //     {
                    //         Console.WriteLine("EndTransaction => Start-Tag not found: '{0}'", transaction.StartTagId);
                    //         // assume "valid" and allow to end the transaction
                    //     }
                    // }

                    // if (valid)
                    // {
                    // write current meter value in "stop" value
                    Console.WriteLine("EndTransaction => Meter='{0}' (kWh)", meterKWH);

                    transaction.StopTime = DateTime.Parse(transactionEventRequest.Timestamp);
                    transaction.MeterStop = meterKWH;
                    transaction.StopTagId = idTag;
                    transaction.StopReason = transactionEventRequest.TriggerReason.ToString();
                    _context.SaveChanges();

                    // Update connecter status to available

                    // }
                }
                else
                {
                    Console.WriteLine(
                        "EndTransaction => Unknown transaction: uid='{0}' / chargepoint='{1}' / tag={2}",
                        transactionEventRequest.TransactionInfo?.TransactionId,
                        chargePointStatus?.Id,
                        idTag
                    );
                    // await _msgLogRepo.SaveLogMessage(ChargePointStatus?.Id, connectorID, msgIn.Action, string.Format("UnknownTransaction:UID={0}/Meter={1}", transactionEventRequest.TransactionInfo?.TransactionId, GetMeterValue(transactionEventRequest.MeterValues)), errorCode);
                    errorCode = ErrorCodes.PropertyConstraintViolation;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("EndTransaction => Exception: {0}", exp.Message);
                Console.WriteLine(exp.StackTrace);
                transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
            }
        }

        public async Task<List<TransactionListDto>> GetChargePointTransactions(string chargePointId)
        {
            var transactions = await _context
                .Transactions.Where(u => u.ChargePointID == chargePointId)
                .OrderByDescending(u => u.StartTime)
                .ToListAsync();
            var transactionsDto = _mapper.Map<List<Transaction>, List<TransactionListDto>>(
                transactions
            );
            return transactionsDto;
        }

        public async Task<AuthorizationStatusEnumType> ValidateCard(string idTag)
        {
            if (string.IsNullOrWhiteSpace(idTag))
            {
                return AuthorizationStatusEnumType.Accepted;
            }
            var card = await _context.Cards.FirstOrDefaultAsync(c => c.CardNumber == idTag);

            if (card == null)
                return AuthorizationStatusEnumType.Unknown;

            if (card.Blocked.HasValue && card.Blocked.Value)
                return AuthorizationStatusEnumType.Blocked;

            if (card.ExpirationDate < DateTime.Now)
                return AuthorizationStatusEnumType.Expired;

            return AuthorizationStatusEnumType.Accepted;
        }
    }
}
