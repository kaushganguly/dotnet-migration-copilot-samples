using System.Collections.Concurrent;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// In-process notification queue used by the admin notification dashboard.
    /// </summary>
    /// <remarks>
    /// The original ASP.NET Framework implementation used MSMQ (<c>System.Messaging</c>),
    /// which is a Windows-only technology with no equivalent in .NET (Core)/.NET 10 and is
    /// not available on Linux. As part of this modernization, the queue has been replaced
    /// with an in-process, thread-safe <see cref="ConcurrentQueue{T}"/> registered as a
    /// singleton service, which preserves the original public API
    /// (<see cref="SendNotification"/>, <see cref="ReceiveNotification"/>, <see cref="MarkAsRead"/>)
    /// so callers did not need to change. Notifications therefore no longer survive an
    /// application restart and are not shared across multiple instances/replicas of the app.
    /// A production deployment that needs durable, cross-process, or cross-machine delivery
    /// should replace this with a real message broker (e.g. Azure Service Bus, RabbitMQ).
    /// </remarks>
    public class NotificationService
    {
        private readonly ConcurrentQueue<Notification> _queue = new();

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string? userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string? entityDisplayName, EntityOperation operation, string? userName = null)
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

        public Notification? ReceiveNotification()
        {
            return _queue.TryDequeue(out var notification) ? notification : null;
        }

        public void MarkAsRead(int notificationId)
        {
            // In a real implementation, you might want to store notifications in database as well
            // for persistence and tracking read status
        }

        private static string GenerateMessage(string entityType, string entityId, string? entityDisplayName, EntityOperation operation)
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
