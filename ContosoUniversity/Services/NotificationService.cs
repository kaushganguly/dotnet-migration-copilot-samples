using System;
using System.Collections.Concurrent;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    // Replaces the original MSMQ (System.Messaging) backed implementation: MSMQ is a
    // Windows-only, COM-based technology with no supported .NET 10 equivalent. This
    // in-process ConcurrentQueue preserves the exact public API surface so callers
    // (BaseController, NotificationsController, Scripts/notifications.js contract)
    // require no changes. Trade-off: notifications no longer survive an app restart
    // and are not shared across multiple app instances/processes.
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

                // Serialize round-trip retained for parity with the previous implementation
                // (also guards against accidentally enqueuing a non-serializable payload).
                var jsonMessage = JsonConvert.SerializeObject(notification);
                var roundTripped = JsonConvert.DeserializeObject<Notification>(jsonMessage);

                _queue.Enqueue(roundTripped);
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
            // No unmanaged resources to release with the in-process queue implementation.
        }
    }
}
