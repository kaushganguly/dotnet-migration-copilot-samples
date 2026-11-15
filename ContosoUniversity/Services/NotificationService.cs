using System;
using System.Collections.Concurrent;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// In-process, cross-platform replacement for the original MSMQ-backed notification
    /// queue (System.Messaging.MessageQueue is Windows-only and has no supported
    /// equivalent on .NET 10 / Linux). Registered as a singleton so all requests in the
    /// process share the same queue, mirroring the original single-machine private-queue
    /// semantics. The public API (method names/signatures) and the JSON message shape are
    /// preserved so controllers and the admin dashboard are unaffected by the swap.
    /// </summary>
    public class NotificationService : IDisposable
    {
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string? userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string? entityDisplayName, EntityOperation operation, string? userName = null)
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

                // Round-trip through JSON (as the original MSMQ implementation did) so the
                // wire/message shape stays identical even though the transport changed.
                var jsonMessage = JsonConvert.SerializeObject(notification);
                _queue.Enqueue(jsonMessage);
            }
            catch (Exception ex)
            {
                // Log error but don't break the main operation
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public Notification? ReceiveNotification()
        {
            try
            {
                if (_queue.TryDequeue(out var jsonContent))
                {
                    return JsonConvert.DeserializeObject<Notification>(jsonContent);
                }

                // No messages available
                return null;
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

        private static string GenerateMessage(string entityType, string entityId, string? entityDisplayName, EntityOperation operation)
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
            // Nothing to dispose - the in-memory queue needs no cleanup.
            GC.SuppressFinalize(this);
        }
    }
}
