using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public partial class VoltaXApiDbContext
    {
        private readonly record struct AccessChanges(List<int> UserIds, bool All)
        {
            public bool Any => All || UserIds.Count > 0;
        }

        /// <summary>Users whose row is about to change, and whether a role, permission or image changes.</summary>
        private AccessChanges CollectAccessChanges()
        {
            if (_accessChanges == null)
                return new AccessChanges(new List<int>(), false);
            var userIds = ChangeTracker.Entries<User>()
                .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
                .Select(e => e.Entity.ID)
                .ToList();
            var all = ChangeTracker.Entries()
                .Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                    && (e.Entity is Role or Permission or RolePermission
                        || e.Entity is Image && e.State is EntityState.Modified or EntityState.Deleted));
            return new AccessChanges(userIds, all);
        }

        private void NotifyAccessChanges(AccessChanges changes)
        {
            if (_accessChanges == null || !changes.Any)
                return;
            if (changes.All)
                _accessChanges.AllChanged();
            else
                _accessChanges.UsersChanged(changes.UserIds);
        }
    }
}
