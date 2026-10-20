# Setup and Testing Guide

## Prerequisites

- .NET 10 SDK
- SQL Server, with a configured `ConnectionStrings:DefaultConnection` value in `appsettings.json` or another ASP.NET Core configuration source

The default connection string uses SQL Server LocalDB, which is available on Windows. The application creates and seeds the database when it starts.

## Build and run

From the `ContosoUniversity` directory:

```sh
dotnet restore
dotnet build
dotnet run
```

## Test notifications manually

1. Run the application against a configured SQL Server database.
2. Open the Notifications page.
3. Create, edit, or delete a student, course, instructor, or department.
4. Verify the notification appears in the dashboard.

The browser polls `/Notifications/GetNotifications` every five seconds. Check the browser console and network panel if notifications do not appear. Notifications are stored in SQL Server and consumed in creation order by the dashboard endpoint.

## Upload configuration

Course image uploads are limited to 5 MB per image and 10 MB per multipart request. The request limit is configured in `Program.cs`; uploaded files are written under `Uploads/TeachingMaterials/`, which must be writable by the application process.
