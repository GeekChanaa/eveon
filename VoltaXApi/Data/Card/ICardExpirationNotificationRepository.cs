using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public interface ICardExpirationNotificationRepository : IRepository<CardExpirationNotification>
    {
        Task CreateCardExpirationNotification(int cardID, string intervalName);
        Task<bool> HasNotificationBeenSentAsync(int cardID, string intervalName);
    }
}