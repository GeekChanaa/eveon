using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Authorization;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : GenericController<Notification>
    {
        private readonly IRepository<Notification> _repository;
        private readonly VoltaXApiDbContext _db;

        public NotificationController(IRepository<Notification> repository, VoltaXApiDbContext db) : base(repository)
        {
            _repository = repository;
            _db = db;
        }

        // Set by DashboardAccessFilter; every endpoint below is scoped to this user.
        private int CurrentUserID => ((AccessSnapshot)HttpContext.Items[typeof(AccessSnapshot)]!).User.ID;

        private IQueryable<Notification> MyNotifications() =>
            _db.Notifications.Where(n => n.ReceiverID == CurrentUserID && !n.IsDeleted);

        [HttpGet("GetMyNotifications")]
        public async Task<ActionResult<NotificationListDto>> GetMyNotifications([FromQuery] int take = 20)
        {
            take = Math.Clamp(take, 1, 100);
            return new NotificationListDto
            {
                UnreadCount = await MyNotifications().CountAsync(n => !n.Read),
                Items = await MyNotifications().AsNoTracking()
                    .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.ID)
                    .Take(take)
                    .Select(n => new NotificationDto
                    {
                        ID = n.ID, Type = n.NotificationType != null ? n.NotificationType.Name : null, Action = n.Action,
                        Description = n.Description, Url = n.Url, Urgent = n.Urgent, Read = n.Read, CreatedAt = n.CreatedAt
                    })
                    .ToListAsync()
            };
        }

        [HttpPut("MarkAsRead/{notificationID}")]
        public async Task<IActionResult> MarkAsRead(int notificationID)
        {
            var updated = await MyNotifications().Where(n => n.ID == notificationID)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true).SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
            return updated == 0 ? NotFound() : NoContent();
        }

        [HttpPut("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await MyNotifications().Where(n => !n.Read)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true).SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
            return NoContent();
        }

        [HttpDelete("DeleteMyNotification/{notificationID}")]
        public async Task<IActionResult> DeleteMyNotification(int notificationID)
        {
            var deleted = await MyNotifications().Where(n => n.ID == notificationID)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsDeleted, true).SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
            return deleted == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("DeleteAllMyNotifications")]
        public async Task<IActionResult> DeleteAllMyNotifications()
        {
            await MyNotifications()
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsDeleted, true).SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
            return NoContent();
        }
    }
}
