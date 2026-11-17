using System;
using System.Collections.Concurrent;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// In-process notification queue.
    ///
    /// The original ASP.NET Framework implementation used a private MSMQ queue
    /// (System.Messaging), which is a Windows-only API with no equivalent in
    /// .NET 10 / ASP.NET Core (System.Messaging does not exist on .NET 5+, and
    /// MSMQ itself is Windows-only infrastructure). Since this queue was only
    /// ever used as a same-process, non-durable hand-off between
    /// SendNotification (called by BaseController after create/update/delete
    /// operations) and ReceiveNotification (polled by
    /// Controllers/NotificationsController.cs, in turn polled by
    /// Scripts/notifications.js every 5 seconds), it is replaced here with an
    /// in-memory, thread-safe queue (ConcurrentQueue) registered as a
    /// singleton service. This preserves the existing public API
    /// (SendNotification/ReceiveNotification/MarkAsRead) and the existing
    /// callers unchanged. Notifications are not persisted across app restarts,
    /// matching the original transient/at-most-delivered-once behavior of a
    /// freshly created, non-transactional MSMQ queue in this sample.
    /// </summary>
    public class NotificationService
    {
        private readonly ConcurrentQueue<Notification> _queue = new ConcurrentQueue<Notification>();

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
        {
            try
            {
                var notification = new Notification
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Operation = operation.ToString(),
                    Message = GenerateMessage(entityType, entityId, entityDisplayName, operation),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                };

                _queue.Enqueue(notification);
            }
            catch (Exception ex)
            {
                // Log error but don't break the main operation
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public Notification ReceiveNotification()
        {
            try
            {
                return _queue.TryDequeue(out var notification) ? notification : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to receive notification: {ex.Message}");
                return null;
            }
        }

        public void MarkAsRead(int notificationId)
        {
            // In a real implementation, you might want to store notifications in database as well
            // for persistence and tracking read status
        }

        private string GenerateMessage(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
        {
            var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
                ? $"{entityType} '{entityDisplayName}'"
                : $"{entityType} (ID: {entityId})";

            switch (operation)
            {
                case EntityOperation.CREATE:
                    return $"New {displayText} has been created";
                case EntityOperation.UPDATE:
                    return $"{displayText} has been updated";
                case EntityOperation.DELETE:
                    return $"{displayText} has been deleted";
                default:
                    return $"{displayText} operation: {operation}";
            }
        }

        public void Dispose()
        {
            // No unmanaged resources to release (ConcurrentQueue requires no disposal).
        }
    }
}
