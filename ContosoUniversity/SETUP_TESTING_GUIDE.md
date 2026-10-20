# Setup and Testing Guide

## Prerequisites

- .NET 10 SDK
- A SQL Server instance supported by your operating system
- A connection string configured as `ConnectionStrings__DefaultConnection` in `appsettings.json` or the environment

## Build and run

```sh
dotnet restore ContosoUniversity.sln
dotnet build ContosoUniversity.sln
dotnet run --project ContosoUniversity.csproj
```

The application initializes the database and adds sample data when the database is empty.

## Test notifications

1. Open the Notifications page from the main menu.
2. Create, edit, or delete a student, course, instructor, or department.
3. Confirm that a notification appears within five seconds.
4. Confirm that the notification can be dismissed and disappears automatically after one minute.

If a notification does not appear, check the browser's network tab for requests to `/Notifications/GetNotifications`, and verify the configured SQL Server connection.
