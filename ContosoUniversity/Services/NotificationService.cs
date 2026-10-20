using System;
using System.Collections.Concurrent;
using System.Threading;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Delivers admin notifications for entity operations.
    /// </summary>
    /// <remarks>
    /// The original implementation relied on MSMQ (System.Messaging), which is a Windows-only
    /// technology that was never ported to .NET Core/.NET 5+ and is unavailable on .NET 10.
    /// This replacement uses an in-memory, thread-safe queue shared via a singleton DI
    /// registration so notifications continue to be visible to every request/admin, the same
    /// way the named MSMQ private queue was shared across requests. Notifications are now
    /// scoped to the lifetime of the running process instead of being durably persisted,
    /// which is the unavoidable behavioral change of dropping the Windows-only MSMQ dependency.
    /// </remarks>
    public class NotificationService : IDisposable
    {
        private readonly ConcurrentQueue<Notification> _queue = new ConcurrentQueue<Notification>();
        private int _nextId;

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
                    Id = Interlocked.Increment(ref _nextId),
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
            _queue.Clear();
        }
    }
}
