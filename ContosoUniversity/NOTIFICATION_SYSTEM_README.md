# Real-Time Admin Notification System

This project now includes a real-time notification system that alerts administrators whenever entity operations (create, update, delete) are performed in the system.

## Overview

The notification system uses an **in-memory, thread-safe queue** (registered as a singleton service via dependency injection) as the underlying technology to provide real-time notifications to administrators. This replaces the original MSMQ-based implementation, which relied on a Windows-only API that is not available on .NET 10.

## Features

- **Real-time notifications**: Admins receive immediate notifications when entities are modified
- **Entity coverage**: Monitors Students, Courses, Instructors, and Departments
- **Operation tracking**: Tracks CREATE, UPDATE, and DELETE operations
- **Admin-only**: Only users with administrator role receive notifications
- **Non-intrusive UI**: Notifications appear in the top-right corner with auto-dismiss
- **In-process delivery**: Uses a singleton in-memory queue shared across requests for the lifetime of the running application

## How It Works

### Backend Components

1. **NotificationService**: Handles in-memory queue operations for sending/receiving messages (registered as a DI singleton)
2. **BaseController**: Base class that all controllers inherit from to send notifications
3. **Notification Model**: Entity to represent notification data
4. **NotificationsController**: API endpoints for retrieving notifications

### Frontend Components

1. **notifications.css**: Styling for notification UI elements
2. **notifications.js**: JavaScript polling system that checks for new notifications
3. **Layout integration**: Admin-only inclusion of notification assets

### Technology Stack

- **In-memory ConcurrentQueue**: Message queue technology (process-lifetime scoped)
- **Entity Framework Core**: Data access
- **ASP.NET Core MVC**: Web framework
- **JavaScript/jQuery**: Frontend polling and UI updates
- **Bootstrap**: UI styling

## Configuration

The notification system is registered as a singleton service in `Program.cs`:

```csharp
builder.Services.AddSingleton<NotificationService>();
```

## Queue Details

- **Queue Type**: In-memory `ConcurrentQueue<Notification>`, scoped to the application process
- **Lifetime**: Singleton — shared across all requests, cleared on application restart (not durably persisted)
- **Message Format**: Strongly typed `Notification` objects, serialized to JSON for the client via `NotificationsController`

## Usage

### For Administrators

1. Log in with an administrator account
2. Navigate to **Notifications** in the main menu to view the dashboard
3. Perform any CRUD operation on entities (Students, Courses, Instructors, Departments)
4. Watch for notifications appearing in the top-right corner
5. Notifications auto-dismiss after 1 minute or can be manually closed

### For Developers

To add notification support to a new controller:

1. Inherit from `BaseController` instead of `Controller`
2. Remove the private `SchoolContext db` declaration (handled by base class)
3. Call `SendEntityNotification()` after successful save operations:

```csharp
// Example: After creating a student
db.Students.Add(student);
db.SaveChanges();
SendEntityNotification("Student", student.ID.ToString(), EntityOperation.CREATE);
```

## Notification Types

- **CREATE**: Green notification for entity creation
- **UPDATE**: Blue notification for entity updates  
- **DELETE**: Orange notification for entity deletion

## System Requirements

- .NET 10.0 SDK/runtime
- SQL Server (for Entity Framework Core)

## Testing the System

1. Access the **Notifications** dashboard from the admin menu
2. Click on any of the "Create new..." buttons provided
3. Complete a create/edit/delete operation
4. Observe the notification appearing in the top-right corner

## Troubleshooting

### Common Issues

1. **No notifications appearing**: Check browser console for JavaScript errors
2. **Notifications missing after a restart**: Expected — the in-memory queue is process-lifetime scoped and is not durably persisted across application restarts

### Development Notes

- Notification sends are wrapped in a try/catch so failures don't affect user operations
- Failed notification sends are logged to debug output but don't affect user operations
- JavaScript polling occurs every 5 seconds
- Maximum of 5 notifications are displayed simultaneously

## Architecture Benefits

- **Decoupled**: The notification queue is isolated from main application request handling
- **Lightweight**: No external messaging infrastructure (e.g., MSMQ, a message broker) required
- **Scalable**: Can easily extend to support multiple administrators
- **Maintainable**: Clear separation between notification logic and business logic

> **Note**: Because the queue is in-memory, notifications are not shared across multiple instances/replicas of the application and do not survive a process restart. For production, multi-instance deployments, consider a durable, distributed solution (e.g., a message broker or SignalR backed by a shared backplane).

## Future Enhancements

Potential improvements for production use:

1. **SignalR integration**: Real-time push notifications instead of polling
2. **Email notifications**: Send email alerts for critical operations
3. **Notification persistence**: Store notifications in database for audit trail
4. **User preferences**: Allow admins to configure notification types
5. **Batch operations**: Group related notifications to reduce noise
6. **Advanced filtering**: Filter notifications by entity type or operation
