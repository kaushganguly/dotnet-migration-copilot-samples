using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly SchoolContext _context;

        public NotificationService(SchoolContext context)
        {
            _context = context;
        }

        public void SendNotification(
            string entityType,
            string entityId,
            EntityOperation operation,
            string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(
            string entityType,
            string entityId,
            string entityDisplayName,
            EntityOperation operation,
            string userName = null)
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

            _context.Notifications.Add(new Notification
            {
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation.ToString(),
                Message = message,
                CreatedAt = DateTime.Now,
                CreatedBy = userName ?? "System",
                IsRead = false
            });
            _context.SaveChanges();
        }

        public Notification ReceiveNotification()
        {
            var notification = _context.Notifications
                .Where(item => !item.IsRead)
                .OrderBy(item => item.CreatedAt)
                .ThenBy(item => item.Id)
                .FirstOrDefault();

            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                _context.SaveChanges();
            }

            return notification;
        }

        public void MarkAsRead(int notificationId)
        {
            var notification = _context.Notifications.Find(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                _context.SaveChanges();
            }
        }
    }
}
