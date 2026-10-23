namespace VoltaXApi.Data
{
    /// <summary>
    /// Told by <see cref="VoltaXApiDbContext"/> after a save that changed what a user may access (the user row, a
    /// role, a permission or an image), so cached access can be dropped. See AccessSnapshotCache.
    /// </summary>
    public interface IAccessChangeListener
    {
        void UsersChanged(IReadOnlyCollection<int> userIds);
        void AllChanged();
    }
}
