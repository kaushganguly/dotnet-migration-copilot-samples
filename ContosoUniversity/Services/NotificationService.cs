using System;
using MSMQ.Messaging;
using Microsoft.Extensions.Options;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly string _queuePath;
        private readonly MessageQueue _queue;
        private readonly bool _queueAvailable;

        public NotificationService(IOptions<MessageQueueOptions> options)
        {
            // Get queue path from configuration (appsettings.json "NotificationQueue:QueuePath")
            // or fall back to the same default used previously under Web.config.
            _queuePath = options?.Value?.QueuePath ?? @".\Private$\ContosoUniversityNotifications";

            try
            {
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
                _queueAvailable = true;
            }
            catch (Exception ex)
            {
                // MSMQ (Microsoft Message Queuing) is a Windows-only feature that must be enabled
                // on the host machine/OS, same requirement this service had under ASP.NET
                // Framework. When it isn't available (e.g. non-Windows hosts, or Windows hosts
                // without the feature installed), degrade gracefully instead of taking down every
                // controller that depends on NotificationService via BaseController.
                System.Diagnostics.Debug.WriteLine($"NotificationService: message queue unavailable, notifications are disabled. {ex.Message}");
                _queueAvailable = false;
            }
        }

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
        {
            if (!_queueAvailable)
            {
                return;
            }

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
            if (!_queueAvailable)
            {
                return null;
            }

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
