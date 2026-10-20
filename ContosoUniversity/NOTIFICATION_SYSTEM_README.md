# Notification system

ASP.NET Core MVC controllers record notifications in the `Notification` table using Entity Framework Core. The notification dashboard polls `Notifications/GetNotifications` every five seconds; retrieved messages are marked as read. Notifications remain in the database across application restarts.

## Monitored operations

Student, course, instructor, and department create, update, and delete operations send a notification after the corresponding database update succeeds. The dashboard displays up to five recent notifications at a time.

## Implementation

- `NotificationService` writes and reads notification entities through the scoped `SchoolContext`.
- `BaseController` provides the shared notification helper used by application controllers.
- `NotificationsController` exposes the polling endpoint and dashboard.
- The UI uses static files under `wwwroot/Content` and `wwwroot/Scripts`.

The application does not configure authentication or role authorization. Secure the dashboard and mutation endpoints through the hosting environment or add an application authentication policy before exposing them publicly.

## Running and testing

Configure `ConnectionStrings:DefaultConnection` in `appsettings.json` (or through environment configuration), then run:

```sh
dotnet test ContosoUniversity.sln
dotnet run --project ContosoUniversity.csproj
```

The default connection string targets SQL Server LocalDB on Windows. The notification tests use an isolated in-memory EF Core database.
