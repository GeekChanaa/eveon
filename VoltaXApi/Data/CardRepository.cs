
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AutoMapper;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public class CardRepository : Repository<Card>, ICardRepository
    {

        private readonly IMapper _mapper;
        public CardRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        // Get all user recharge cards
        public async Task<List<Card>> GetUserRechargeCardsAsync(int UserID)
        {
            return await _context.Cards.Where(u => u.UserID == UserID).ToListAsync();
        }

        // get card transactions
        public async Task<List<TransactionDto>> GetCardTransactions(int CardID)
        {
            var card = await _context.Cards.Include(u => u.Transactions).FirstOrDefaultAsync(u => u.ID == CardID);
            var transactions = card?.Transactions.AsQueryable();
            return _mapper.ProjectTo<TransactionDto>(transactions).ToList();
        }

        // get card Orders
        public async Task<List<OrderDto>> GetCardOrders(int CardID)
        {
            var card = await _context.Cards.Include(u => u.Orders).FirstOrDefaultAsync(u => u.ID == CardID);
            var Orders = card?.Orders.AsQueryable();
            return _mapper.ProjectTo<OrderDto>(Orders).ToList();
        }

        // Get Card with its transactions and orders
        public async Task<CardWithTransactionsOrdersDto> GetCardByID(int CardID)
        {
            var card = await _context.Cards
                .Include(u => u.Transactions)
                    .ThenInclude(u => u.ChargePoint)
                .Include(u => u.Orders).FirstOrDefaultAsync(u => u.ID == CardID);

            var cardDto = new CardWithTransactionsOrdersDto
            {
                ID = card.ID,
                CardNumber = card.CardNumber,
                CardType = card.CardType,
                ExpirationDate = card.ExpirationDate,
                MaxCount = card.MaxCount,
                Status = card.Status,
                Balance = card.Balance,
                Note = card.Note,
                UserID = card.UserID,
                Orders = card.Orders.Select(o => new CardOrderDto
                {
                    ID = o.ID,
                    Amount = o.Amount,
                    RechargeDate = o.RechargeDate
                }).ToList(),
                Transactions = card.Transactions.Select(t => new CardTransactionDto
                {
                    ID = t.ID,
                    Uid = t.Uid,
                    ChargePointID = t.ChargePoint.ChargePointId,
                    ConnectorID = t.ConnectorID,
                    StartTagId = t.StartTagId,
                    StartTime = t.StartTime,
                    MeterStart = t.MeterStart,
                    StartResult = t.StartResult,
                    StopTagId = t.StopTagId,
                    StopTime = t.StopTime,
                    MeterStop = t.MeterStop,
                    StopReason = t.StopReason,
                    Amount = t.Amount
                }).ToList()
            };

            return cardDto;
        }



    }
}