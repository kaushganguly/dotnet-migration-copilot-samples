using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly SchoolContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(SchoolContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
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
            try
            {
                _context.Notifications.Add(new Notification
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Operation = operation.ToString(),
                    Message = GenerateMessage(entityType, entityId, entityDisplayName, operation),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                });
                _context.SaveChanges();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send a notification for {EntityType} {EntityId}", entityType, entityId);
            }
        }

        public Notification ReceiveNotification()
        {
            try
            {
                var notification = _context.Notifications
                    .OrderBy(item => item.CreatedAt)
                    .ThenBy(item => item.Id)
                    .FirstOrDefault();

                if (notification == null)
                {
                    return null;
                }

                _context.Notifications.Remove(notification);
                _context.SaveChanges();
                return notification;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to retrieve a notification");
                return null;
            }
        }

        public void MarkAsRead(int notificationId)
        {
        }

        private static string GenerateMessage(
            string entityType,
            string entityId,
            string entityDisplayName,
            EntityOperation operation)
        {
            var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
                ? $"{entityType} '{entityDisplayName}'"
                : $"{entityType} (ID: {entityId})";

            return operation switch
            {
                EntityOperation.CREATE => $"New {displayText} has been created",
                EntityOperation.UPDATE => $"{displayText} has been updated",
                EntityOperation.DELETE => $"{displayText} has been deleted",
                _ => $"{displayText} operation: {operation}"
            };
        }
    }
}
