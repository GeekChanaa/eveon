using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface INotificationSettingRepository : IRepository<NotificationSetting>
    {
        Task<List<NotificationSetting>> GetUserNotificationSettings(int UserID);
    }
}