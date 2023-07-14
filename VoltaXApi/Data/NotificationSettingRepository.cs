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




    }
}