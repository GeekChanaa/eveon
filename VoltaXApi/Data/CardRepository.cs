
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
        public async Task<List<Card>> GetUserRechargeCardsAsync(int UserID)
        {
            return await _context.Cards.Where(u => u.UserID == UserID).ToListAsync();
        }

    }
}