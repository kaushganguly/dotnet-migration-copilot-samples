# Real-Time Admin Notification System

This project now includes a real-time notification system that alerts administrators whenever entity operations (create, update, delete) are performed in the system.

## Overview

The notification system uses an **in-process `ConcurrentQueue<Notification>` singleton** (registered via dependency injection) as the underlying technology to provide real-time notifications to administrators.

> **Migration note**: As part of the upgrade from .NET Framework 4.8 to .NET 10, MSMQ (`System.Messaging`) was replaced with an in-memory, non-durable `ConcurrentQueue<Notification>` singleton. MSMQ is a Windows-only technology with no equivalent in modern .NET, and the original usage here was a simple, non-durable, in-process hand-off (no persistence or cross-process delivery was relied upon), so the in-memory queue preserves identical behavior for this application while removing the Windows-only dependency.

## Features

- **Real-time notifications**: Admins receive immediate notifications when entities are modified
- **Entity coverage**: Monitors Students, Courses, Instructors, and Departments
- **Operation tracking**: Tracks CREATE, UPDATE, and DELETE operations
- **Admin-only**: Only users with administrator role receive notifications
- **Non-intrusive UI**: Notifications appear in the top-right corner with auto-dismiss
- **In-process delivery**: Uses an in-memory queue for low-latency, same-process delivery

## How It Works

### Backend Components

1. **NotificationService**: Handles enqueue/dequeue operations via an in-memory `ConcurrentQueue<Notification>` (DI singleton)
2. **BaseController**: Base class that all controllers inherit from to send notifications
3. **Notification Model**: Entity to represent notification data
4. **NotificationsController**: API endpoints for retrieving notifications

### Frontend Components

1. **notifications.css**: Styling for notification UI elements
2. **notifications.js**: JavaScript polling system that checks for new notifications
3. **Layout integration**: Admin-only inclusion of notification assets

### Technology Stack

- **ConcurrentQueue<Notification>**: In-memory, thread-safe queue (DI singleton)
- **Entity Framework Core**: Data access for notification persistence
- **ASP.NET Core MVC**: Web framework
- **JavaScript/jQuery**: Frontend polling and UI updates
- **Bootstrap**: UI styling

## Configuration

`NotificationService` is registered as a singleton in `Program.cs`:

```csharp
builder.Services.AddSingleton<NotificationService>();
```

## Queue Details

- **Queue Type**: In-memory `ConcurrentQueue<Notification>`, scoped to the application's lifetime (non-durable — cleared on app restart)
- **Message Format**: `Notification` objects (no serialization required, in-process only)

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
2. **Notifications missing after restart**: Expected — the in-memory queue is non-durable and clears when the application restarts

### Development Notes

- Notifications are sent synchronously to the in-memory queue and won't throw or block main operations under normal conditions
- Failed notification sends are logged via `ILogger`/debug output but don't affect user operations
- JavaScript polling occurs every 5 seconds
- Maximum of 5 notifications are displayed simultaneously

## Architecture Benefits

- **Decoupled**: Notification sends don't affect main application performance
- **Lightweight**: No external messaging infrastructure (MSMQ, broker, etc.) required
- **Scalable**: Can easily extend to support multiple administrators
- **Maintainable**: Clear separation between notification logic and business logic

> Note: Unlike the original MSMQ-based implementation, the in-memory queue does **not** persist notifications across application restarts. If durable, cross-process, or cross-instance delivery is required in the future, consider replacing the in-memory queue with Azure Service Bus, Azure Queue Storage, or SignalR (see Future Enhancements below).

## Future Enhancements

Potential improvements for production use:

1. **SignalR integration**: Real-time push notifications instead of polling
2. **Email notifications**: Send email alerts for critical operations
3. **Notification persistence**: Store notifications in database for audit trail
4. **User preferences**: Allow admins to configure notification types
5. **Batch operations**: Group related notifications to reduce noise
6. **Advanced filtering**: Filter notifications by entity type or operation
