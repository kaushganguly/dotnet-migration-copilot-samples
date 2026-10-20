# Notification System

The application records notifications in the `Notification` table using Entity Framework Core. Creating, updating, or deleting a student, course, instructor, or department creates a notification; the browser polls for unread notifications every five seconds and displays up to five at a time.

## Implementation

- `NotificationService` writes notifications and retrieves up to ten unread notifications per poll.
- `BaseController` sends notifications after successful entity changes.
- `NotificationsController` exposes the polling and mark-as-read endpoints.
- `Scripts/notifications.js` safely renders notification text and handles dismissal.

The notification table is created with the rest of the database by the EF Core initializer. Configure the SQL Server connection in `appsettings.json` or the `ConnectionStrings__DefaultConnection` environment variable. MSMQ and Windows-specific queue setup are not required.

## Testing

Open the Notifications page and create, edit, or delete a student, course, instructor, or department. A corresponding notification should appear within the next polling interval and disappear automatically after one minute.
