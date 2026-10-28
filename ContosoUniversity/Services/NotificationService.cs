using System;
using System.Threading.Channels;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// In-process notification queue. Replaces the original MSMQ
    /// (System.Messaging.MessageQueue) implementation, which is Windows-only
    /// and unavailable on modern .NET. The public surface (SendNotification,
    /// ReceiveNotification, MarkAsRead, Dispose) is preserved so BaseController
    /// and NotificationsController require no behavioral changes.
    /// Registered as a singleton in Program.cs so the channel is shared across requests.
    /// </summary>
    public class NotificationService : IDisposable
    {
        private readonly Channel<string> _channel;

        public NotificationService()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<string>(options);
        }

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

                var jsonMessage = JsonConvert.SerializeObject(notification);
                _channel.Writer.TryWrite(jsonMessage);
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
                if (_channel.Reader.TryRead(out var jsonContent))
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
            _channel.Writer.TryComplete();
        }
    }
}
