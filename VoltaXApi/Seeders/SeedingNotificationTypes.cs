

using System;
using VoltaXApi.Data;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VoltaXApi.Helpers
{
    public static class SeedingNotificationTypes
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                context.NotificationTypes.Add(new NotificationType{Name="Default",Description="Default Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="User",Description="User Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="Charging Station",Description="Charging Station Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="Charge Point",Description="Charge Point Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="Connector",Description="Connector Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="Charging Cards",Description="Charging Cards Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="Transaction",Description="Transaction Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="Recharge Orders",Description="Recharge Orders Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.NotificationTypes.Add(new NotificationType{Name="Comment",Description="Comment Notifications",ForAdmins = true, ForPartners = false,ForCustomers = false});
                context.SaveChanges();
                
            }
        }
    }
}