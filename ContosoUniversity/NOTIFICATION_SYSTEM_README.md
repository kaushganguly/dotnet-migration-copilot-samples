# Notification system

The application records create, update, and delete notifications for students,
courses, instructors, and departments in the EF Core `Notification` table.
`NotificationService` persists events and returns up to ten unread events to
the polling endpoint; the browser polls every five seconds.

## Components

- `NotificationService` persists and reads notification records.
- `BaseController` sends notifications after successful entity operations.
- `NotificationsController` exposes the polling and mark-read endpoints.
- `wwwroot/Scripts/notifications.js` renders notifications in the browser.

The notification service uses the same SQL Server connection as the school
data. No separate message broker or queue configuration is required. The
application currently has no authentication or role-based authorization
configured, so the notification endpoint should be protected by an
authentication policy before deployment where notifications are sensitive.
