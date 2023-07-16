using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Dtos;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class NotificationSettingRepository : Repository<NotificationSetting>, INotificationSettingRepository
    {
        public NotificationSettingRepository(VoltaXApiDbContext context ) : base(context)
        {

        }

        public async Task<List<NotificationSetting>> GetUserNotificationSettings(int UserID)
        {
            return await this._context.NotificationSettings.Where(u => u.UserID == UserID).ToListAsync();
        }

        public async Task<NotificationSetting> GetUserNotificationSetting(int userID, int notificationTypeID)
        {
            return await this._context.NotificationSettings.FirstOrDefaultAsync(u => u.NotificationTypeID == notificationTypeID && u.UserID == userID);
        }



    }
}