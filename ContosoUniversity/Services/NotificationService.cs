using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly SchoolContext _db;

        public NotificationService(SchoolContext db)
        {
            _db = db;
        }

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
        {
            var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
                ? $"{entityType} '{entityDisplayName}'"
                : $"{entityType} (ID: {entityId})";
            var message = operation switch
            {
                EntityOperation.CREATE => $"New {displayText} has been created",
                EntityOperation.UPDATE => $"{displayText} has been updated",
                EntityOperation.DELETE => $"{displayText} has been deleted",
                _ => $"{displayText} operation: {operation}"
            };

            _db.Notifications.Add(new Notification
            {
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation.ToString(),
                Message = message,
                CreatedAt = DateTime.Now,
                CreatedBy = userName ?? "System",
                IsRead = false
            });
            _db.SaveChanges();
        }

        public List<Notification> GetUnreadNotifications(int limit = 10)
        {
            var notifications = _db.Notifications
                .Where(notification => !notification.IsRead)
                .OrderByDescending(notification => notification.CreatedAt)
                .Take(limit)
                .ToList();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }

            if (notifications.Count > 0)
            {
                _db.SaveChanges();
            }

            return notifications;
        }

        public void MarkAsRead(int notificationId)
        {
            var notification = _db.Notifications.Find(notificationId);
            if (notification == null || notification.IsRead)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
            _db.SaveChanges();
        }
    }
}
