# Setup and Testing Guide

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB or another SQL Server instance

Configure the database connection in `appsettings.json` or set
`ConnectionStrings__DefaultConnection` in the environment. The application creates
and seeds the database on startup.

## Build and run

From the `ContosoUniversity` directory:

```sh
dotnet build
dotnet run
```

Open the Notifications page, then create, update, or delete a student, course,
instructor, or department. Notifications are polled every five seconds. The
`/Notifications/GetNotifications` endpoint returns pending notifications and marks
them as read.

## Troubleshooting

- If startup fails while connecting to SQL Server, verify the connection string
  and that the database server is available.
- If notifications do not appear, check the browser console and verify that
  `/Notifications/GetNotifications` returns a successful response.
- Verify `wwwroot/Scripts/notifications.js` and
  `wwwroot/Content/notifications.css` are available as static files.
