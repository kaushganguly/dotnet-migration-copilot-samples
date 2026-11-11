using System;
using System.Threading.Channels;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Sends and receives admin notifications for entity create/update/delete operations.
    /// </summary>
    /// <remarks>
    /// The original .NET Framework implementation used MSMQ (System.Messaging). MSMQ is a
    /// Windows-only technology with no cross-platform story, and the System.Messaging
    /// compatibility shim is no longer shipped for modern .NET (it was removed from the
    /// Microsoft.Windows.Compatibility pack as of .NET 10/11 - there is no supported way to
    /// reference System.Messaging on .NET 10 on any OS). This migration replaces the MSMQ
    /// queue with an in-process, cross-platform substitute built on System.Threading.Channels,
    /// preserving the original public API (SendNotification/ReceiveNotification/MarkAsRead) so
    /// callers (controllers, the Notifications polling endpoint, notifications.js) are unaffected.
    /// Notifications are only retained in-memory for the lifetime of the process.
    /// </remarks>
    public class NotificationService : IDisposable
    {
        private static readonly Channel<Notification> Queue = Channel.CreateUnbounded<Notification>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

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

                Queue.Writer.TryWrite(notification);
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
                return Queue.Reader.TryRead(out var notification) ? notification : null;
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
            // Nothing to dispose: the underlying channel is process-wide and shared
            // across all NotificationService instances/scopes.
        }
    }
}
