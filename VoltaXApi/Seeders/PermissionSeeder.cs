using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders;

public static class PermissionSeeder
{
    public static IEnumerable<Permission> GetPermissionsFromEnum()
    {
        return Enum.GetValues<PermissionEnum>()
            .Select(e => new Permission
            {
                Name = e.ToString(),
                Description = GetDescription(e)
            });
    }

    public static async Task<List<Permission>> Seed(VoltaXApiDbContext context)
        {
            IRepository<Permission> _permissionRepo = new Repository<Permission>(context);

            var existingPermissions = await context.Permissions
                .Select(p => p.Name)
                .ToListAsync();

            var allPermissions = Enum.GetValues<PermissionEnum>()
                .Select(e => new Permission
                {
                    Name = e.ToString(),
                    Description = GetDescription(e)
                })
                .Where(p => !existingPermissions.Contains(p.Name)) // Avoid duplicates
                .ToList();

            if (allPermissions.Any())
            {
                await _permissionRepo.AddRangeAsync(allPermissions);
                Console.WriteLine($"Seeded {allPermissions.Count} new permissions.");
            }
            else
            {
                Console.WriteLine("No new permissions to seed.");
            }

            return allPermissions;
        }

    private static string GetDescription(PermissionEnum permission)
    {
        return permission switch
        {
            PermissionEnum.ViewChargingStations => "Can view charging stations",
            PermissionEnum.CreateChargingStations => "Can create charging stations",
            PermissionEnum.EditChargingStations => "Can edit charging stations",
            PermissionEnum.DeleteChargingStations => "Can delete charging stations",

            PermissionEnum.ViewChargePoints => "Can view charge points",
            PermissionEnum.CreateChargePoints => "Can create charge points",
            PermissionEnum.EditChargePoints => "Can edit charge points",
            PermissionEnum.DeleteChargePoints => "Can delete charge points",

            PermissionEnum.ViewChargePointConfiguration => "Can view charge point configuration",
            PermissionEnum.EditChargePointConfiguration => "Can edit charge point configuration",

            PermissionEnum.ViewChargingCards => "Can view charging cards",
            PermissionEnum.CreateChargingCards => "Can create charging cards",
            PermissionEnum.EditChargingCards => "Can edit charging cards",
            PermissionEnum.DeleteChargingCards => "Can delete charging cards",

            PermissionEnum.ViewNotices => "Can view notices",
            PermissionEnum.CreateNotices => "Can create notices",
            PermissionEnum.EditNotices => "Can edit notices",
            PermissionEnum.DeleteNotices => "Can delete notices",

            PermissionEnum.ViewReports => "Can view reports",
            PermissionEnum.CreateReports => "Can create reports",
            PermissionEnum.EditReports => "Can edit reports",
            PermissionEnum.DeleteReports => "Can delete reports",

            PermissionEnum.ViewUserInfoDownloadRequests => "Can view user info download requests",
            PermissionEnum.CreateUserInfoDownloadRequests => "Can create user info download requests",
            PermissionEnum.EditUserInfoDownloadRequests => "Can edit user info download requests",
            PermissionEnum.DeleteUserInfoDownloadRequests => "Can delete user info download requests",

            PermissionEnum.ViewChargingSessions => "Can view charging sessions",
            PermissionEnum.CreateChargingSessions => "Can create charging sessions",
            PermissionEnum.EditChargingSessions => "Can edit charging sessions",
            PermissionEnum.DeleteChargingSessions => "Can delete charging sessions",

            PermissionEnum.ViewTransactions => "Can view transactions",
            PermissionEnum.CreateTransactions => "Can create transactions",
            PermissionEnum.EditTransactions => "Can edit transactions",
            PermissionEnum.DeleteTransactions => "Can delete transactions",

            PermissionEnum.ViewRechargeOrders => "Can view recharge orders",
            PermissionEnum.CreateRechargeOrders => "Can create recharge orders",
            PermissionEnum.EditRechargeOrders => "Can edit recharge orders",
            PermissionEnum.DeleteRechargeOrders => "Can delete recharge orders",

            PermissionEnum.ViewSystemReports => "Can view system reports",
            PermissionEnum.CreateSystemReports => "Can create system reports",
            PermissionEnum.EditSystemReports => "Can edit system reports",
            PermissionEnum.DeleteSystemReports => "Can delete system reports",

            PermissionEnum.ViewUsers => "Can view users",
            PermissionEnum.CreateUsers => "Can create users",
            PermissionEnum.EditUsers => "Can edit users",
            PermissionEnum.DeleteUsers => "Can delete users",

            PermissionEnum.ViewPartners => "Can view partners",
            PermissionEnum.CreatePartners => "Can create partners",
            PermissionEnum.EditPartners => "Can edit partners",
            PermissionEnum.DeletePartners => "Can delete partners",

            PermissionEnum.ViewStatisticsPage => "Can view statistics page",

            _ => permission.ToString()
        };
    }
}
