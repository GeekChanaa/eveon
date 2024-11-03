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
        Task<List<TransactionDto>> GetCardTransactions(int CardID);
        Task<List<OrderDto>> GetCardOrders(int CardID);
        Task<CardWithTransactionsOrdersDto> GetCardByID(int CardID);
        Task<Card> GetCardByNumber(string CardNumber);
        Task CreateCard(CreateCardDto card);
        Task<CardListDto> GetCardForDisplayByID(int cardID);

    }
}