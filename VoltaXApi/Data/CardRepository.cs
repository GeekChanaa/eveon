
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
        public async Task<List<Transaction>> GetCardTransactions(int CardID)
        {
            var card = await _context.Cards.Include(u => u.Transactions).FirstOrDefaultAsync(u => u.ID == CardID);
            return card?.Transactions.ToList();
        }    

        // get card Orders
        public async Task<List<Order>> GetCardOrders(int CardID)
        {
            var card = await _context.Cards.Include(u => u.Orders).FirstOrDefaultAsync(u => u.ID == CardID);
            return card?.Orders.ToList();
        }     


    }
}