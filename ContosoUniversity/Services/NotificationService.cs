using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Services;

public class NotificationService
{
    private readonly SchoolContext _context;

    public NotificationService(SchoolContext context)
    {
        _context = context;
    }

    public void SendNotification(string entityType, string entityId, EntityOperation operation, string? userName = null)
    {
        SendNotification(entityType, entityId, null, operation, userName);
    }

    public void SendNotification(string entityType, string entityId, string? entityDisplayName, EntityOperation operation, string? userName = null)
    {
        var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
            ? $"{entityType} '{entityDisplayName}'"
            : $"{entityType} (ID: {entityId})";
        var message = operation switch
        {
            EntityOperation.CREATE => $"New {displayText} has been created",
            EntityOperation.UPDATE => $"{displayText} has been updated",
            EntityOperation.DELETE => $"{displayText} has been deleted",
            _ => $"{displayText} operation: {operation}"
        };

        _context.Notifications.Add(new Notification
        {
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation.ToString(),
            Message = message,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userName ?? "System",
            IsRead = false
        });
        _context.SaveChanges();
    }

    public IReadOnlyList<Notification> ReceiveNotifications()
    {
        var notifications = _context.Notifications
            .Where(notification => !notification.IsRead)
            .OrderBy(notification => notification.CreatedAt)
            .Take(10)
            .ToList();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        if (notifications.Count > 0)
        {
            _context.SaveChanges();
        }

        return notifications;
    }

    public void MarkAsRead(int notificationId)
    {
        var notification = _context.Notifications
            .FirstOrDefault(item => item.Id == notificationId);
        if (notification is null)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        _context.SaveChanges();
    }
}
