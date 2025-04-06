
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AutoMapper;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public class CardExpirationNotificationRepository : Repository<CardExpirationNotification>, ICardExpirationNotificationRepository
    {

        private readonly IMapper _mapper;
        public CardExpirationNotificationRepository(
            VoltaXApiDbContext context, 
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task CreateCardExpirationNotification(int cardID, string intervalName)
        {
            var notification = new CardExpirationNotification
            {
                CardID = cardID,
                IntervalName = intervalName,
                SentDate = DateTime.UtcNow
            };
            
            _context.CardExpirationNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasNotificationBeenSentAsync(int cardID, string intervalName)
        {
             var notification = await _context.CardExpirationNotifications
                .FirstOrDefaultAsync(n => n.CardID == cardID && n.IntervalName == intervalName);
            
            return notification != null;
        }
    }
}