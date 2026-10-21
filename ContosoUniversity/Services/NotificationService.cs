using System;
using ContosoUniversity.Models;
using Microsoft.Extensions.Options;
using MSMQ.Messaging;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Sends and receives entity-change notifications over MSMQ.
    /// Migrated from System.Messaging (Windows-only, not available on .NET Core) to the
    /// MSMQ.Messaging NuGet package per the migrating-to-msmq-messaging skill. The API surface
    /// (MessageQueue/Message/XmlMessageFormatter) is unchanged - only the namespace differs -
    /// so behavior is preserved. The queue path now comes from configuration (appsettings.json
    /// "NotificationQueue:QueuePath") via the options pattern instead of
    /// ConfigurationManager.AppSettings, which is unavailable outside System.Web/.NET Framework.
    /// </summary>
    public class NotificationService : IDisposable
    {
        private readonly string _queuePath;
        private readonly MessageQueue _queue;

        public NotificationService(IOptions<NotificationQueueOptions> options)
        {
            _queuePath = options?.Value?.QueuePath ?? @".\Private$\ContosoUniversityNotifications";

            // Ensure the queue exists
            if (!MessageQueue.Exists(_queuePath))
            {
                _queue = MessageQueue.Create(_queuePath);
                _queue.SetPermissions("Everyone", MessageQueueAccessRights.FullControl);
            }
            else
            {
                _queue = new MessageQueue(_queuePath);
            }

            // Configure queue formatter
            _queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
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
                var message = new Message(jsonMessage)
                {
                    Label = $"{entityType} {operation}",
                    Priority = MessagePriority.Normal
                };

                _queue.Send(message);
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
                var message = _queue.Receive(TimeSpan.FromSeconds(1));
                var jsonContent = message.Body.ToString();
                return JsonConvert.DeserializeObject<Notification>(jsonContent);
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
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
            _queue?.Dispose();
        }
    }
}
