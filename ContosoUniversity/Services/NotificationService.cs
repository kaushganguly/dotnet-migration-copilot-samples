using System;
using MSMQ.Messaging;
using ContosoUniversity.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Sends and receives admin notifications via MSMQ. Migrated from System.Messaging
    /// (Windows-only, .NET Framework) to MSMQ.Messaging, the drop-in replacement for
    /// .NET Core/.NET 5+ per the migrating-to-msmq-messaging migration pattern.
    ///
    /// Queue connectivity is established lazily (on first send/receive) rather than in the
    /// constructor. MSMQ is a Windows-only facility; this keeps the rest of the application
    /// usable on non-Windows hosts (e.g. Linux containers) even though notification
    /// send/receive will not function there, matching the original app's platform
    /// constraints without taking down every page that constructs a controller.
    /// </summary>
    public class NotificationService : IDisposable
    {
        private readonly string _queuePath;
        private MessageQueue? _queue;

        public NotificationService(IOptions<MessageQueueOptions> options)
        {
            _queuePath = options.Value.QueuePath;
        }

        private MessageQueue Queue => _queue ??= CreateQueue(_queuePath);

        private static MessageQueue CreateQueue(string queuePath)
        {
            MessageQueue queue;
            if (!MessageQueue.Exists(queuePath))
            {
                queue = MessageQueue.Create(queuePath);
                queue.SetPermissions("Everyone", MessageQueueAccessRights.FullControl);
            }
            else
            {
                queue = new MessageQueue(queuePath);
            }

            queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
            return queue;
        }

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

                var jsonMessage = JsonConvert.SerializeObject(notification);
                var message = new Message(jsonMessage)
                {
                    Label = $"{entityType} {operation}",
                    Priority = MessagePriority.Normal
                };

                Queue.Send(message);
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
                var message = Queue.Receive(TimeSpan.FromSeconds(1));
                var jsonContent = message.Body?.ToString();
                return jsonContent == null ? null : JsonConvert.DeserializeObject<Notification>(jsonContent);
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
            _queue?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
