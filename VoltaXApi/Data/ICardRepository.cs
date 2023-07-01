using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public interface ICardRepository : IRepository<Card>
    {
        Task<List<Card>> GetUserRechargeCardsAsync(int UserID);
        Task<List<Transaction>> GetCardTransactions(int CardID);
        Task<List<Order>> GetCardOrders(int CardID);

    }
}