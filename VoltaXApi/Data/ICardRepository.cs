using VoltaXApi.Models;
namespace VoltaXApi.Data
{
    public interface ICardRepository : IRepository<Card>
    {
        Task<List<Card>> GetUserRechargeCardsAsync(int UserID);
    }
}