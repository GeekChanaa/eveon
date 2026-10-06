using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;
using VoltaXApi.Data;

namespace VoltaXApi.Data.Seeders
{
    public static class RoleSeeder
    {
        public static async Task<List<Role>> Seed(VoltaXApiDbContext context)
        {
            IRepository<Role> _roleRepo = new Repository<Role>(context);
            IRepository<RolePermission> _rolePermissionRepo = new Repository<RolePermission>(context);

            var existingRoles = await context.Roles.Select(r => r.Name).ToListAsync();
            var allPermissions = await context.Permissions.ToListAsync();

            var rolesToSeed = new List<Role>();

            // Admin Role with all permissions
            if (!existingRoles.Contains("Admin"))
            {
                var adminRole = new Role
                {
                    Name = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                adminRole.RolePermissions = allPermissions
                    .Select(p => new RolePermission
                    {
                        Permission = p
                    }).ToList();

                rolesToSeed.Add(adminRole);
            }

            // Customer Role with no permissions
            if (!existingRoles.Contains("Customer"))
            {
                var customerRole = new Role
                {
                    Name = "Customer",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                rolesToSeed.Add(customerRole);
            }

            // Partner Role: access is granted by role name and PartnerID, not permissions
            if (!existingRoles.Contains("Partner"))
            {
                rolesToSeed.Add(new Role
                {
                    Name = "Partner",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            if (rolesToSeed.Any())
            {
                await _roleRepo.AddRangeAsync(rolesToSeed);
                Console.WriteLine($"Seeded {rolesToSeed.Count} roles: {string.Join(", ", rolesToSeed.Select(r => r.Name))}");
            }
            else
            {
                Console.WriteLine("No new roles to seed.");
            }

            return rolesToSeed;
        }
    }
}
