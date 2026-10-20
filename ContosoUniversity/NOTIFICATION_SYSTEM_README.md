# Notification System

The application records student, course, instructor, and department create, update,
and delete events in the `Notification` table using Entity Framework Core.
The notification dashboard polls the application every five seconds and displays
up to five notifications at a time.

## Components

- `NotificationService` writes and retrieves notifications through `SchoolContext`.
- `BaseController` sends notifications after successful entity operations.
- `NotificationsController` provides the dashboard and polling endpoints.
- `wwwroot/Scripts/notifications.js` and `wwwroot/Content/notifications.css` render
  and style notifications.

Retrieving notifications marks them as read, preventing the polling client from
displaying the same event repeatedly. Database persistence replaces the former
Windows-only MSMQ dependency.

## Configuration

Configure the SQL Server connection in `appsettings.json` or override it with the
`ConnectionStrings__DefaultConnection` environment variable. The database is
created and seeded at application startup.

## Development

From the `ContosoUniversity` directory, run:

```sh
dotnet run
```

Entity controllers should inherit from `BaseController` and call
`SendEntityNotification()` after saving a successful operation.
