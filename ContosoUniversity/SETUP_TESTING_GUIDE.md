# Build and testing

## Prerequisites

- .NET 10 SDK
- SQL Server (the default LocalDB connection is for Windows; configure a different SQL Server connection in `appsettings.json` when needed)

## Build and run

From the project directory:

```sh
dotnet restore ContosoUniversity.sln
dotnet test ContosoUniversity.sln
dotnet run --project ContosoUniversity.csproj
```

The application initializes the database at startup. Ensure the configured SQL Server is available and that its account can create or update the application database.

## Notification flow

Create, update, or delete a student, course, instructor, or department. The notification dashboard polls the database-backed notification endpoint every five seconds. A notification is marked read when delivered. The notification service tests use EF Core's in-memory provider and do not require SQL Server.

## Teaching-material uploads

Course images must be JPG, JPEG, PNG, GIF, or BMP files smaller than 5 MB. They are stored under `wwwroot/Uploads/TeachingMaterials`; ensure the application identity can write there. The application limits request and multipart bodies to 10 MB.
