
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public class CardRepository : Repository<Card> , ICardRepository 
    {

        public CardRepository(VoltaXApiDbContext context) : base(context)
        {

        }

        // Get all user recharge cards
        public async Task<List<Card>> GetCustomerRechargeCardsAsync(int CustomerID)
        {
            return await _context.Cards.Where(u => u.CustomerID == CustomerID).ToListAsync();
        }

    }
}