# Notification System

The notification feature records entity create, update, and delete events in the `Notification` table and serves them through the MVC notifications endpoint. The dashboard polls `/Notifications/GetNotifications` every five seconds and displays up to five notifications at a time.

## Components

- `NotificationService` stores notifications using the request's EF Core `SchoolContext`.
- `BaseController` provides notification helpers to the CRUD controllers.
- `NotificationsController` returns notifications and the dashboard view.
- `Scripts/notifications.js` polls for notifications and renders the UI.

Notifications are consumed from the database in creation order when fetched. SQL Server and a valid `ConnectionStrings:DefaultConnection` setting are required. The default development connection uses SQL Server LocalDB on Windows; configure another SQL Server connection for other environments.

## Testing manually

1. Run the application against a configured SQL Server database.
2. Open the Notifications page.
3. Create, update, or delete a student, course, instructor, or department.
4. Verify the corresponding notification appears on the page.

The application does not currently configure authentication or role-based authorization; the dashboard and CRUD pages are available according to the application's existing public-access behavior.
