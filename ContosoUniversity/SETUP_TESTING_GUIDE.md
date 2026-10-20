# Setup and testing guide

## Prerequisites

- .NET 10 SDK
- A SQL Server instance accessible through the connection string in
  `appsettings.json` or `ConnectionStrings__DefaultConnection`

## Build and run

```sh
dotnet restore ContosoUniversity.sln
dotnet build ContosoUniversity.sln
dotnet run --project ContosoUniversity.csproj
```

The application creates and seeds its database on startup. The local
`(LocalDb)\\MSSQLLocalDB` connection string is intended for Windows LocalDB;
override it when running against another SQL Server.

## Test notifications

Open the Notifications page, create or modify a student, course, instructor, or
department, and wait for the next five-second browser poll. Notifications are
stored in the database and served by `/Notifications/GetNotifications`.

## Test course image uploads

Course images may be JPG, JPEG, PNG, GIF, or BMP and must be no larger than
5 MB. The application stores uploads in `wwwroot/Uploads/TeachingMaterials`;
the multipart request limit is 10 MB.
