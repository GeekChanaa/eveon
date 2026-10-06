namespace VoltaXApi.Services
{
    /// <param name="Type">NotificationType name; created on first use.</param>
    /// <param name="Action">Machine-readable event, e.g. "ChargePointOffline".</param>
    /// <param name="ActionOn">ID of the entity the event is about.</param>
    public sealed record DashboardNotification(string Type, string Action, string Description, string? ActionOn = null, bool Urgent = false);

    public interface INotificationService
    {
        /// <summary>
        /// Stores and pushes a notification to every dashboard user holding <paramref name="permission"/>,
        /// plus the users of <paramref name="partnerId"/> when given. Never throws: a failed notification
        /// must not fail the operation that raised it.
        /// </summary>
        Task NotifyDashboardAsync(DashboardNotification notification, string permission, string url, int? partnerId = null, string? partnerUrl = null);
    }
}
