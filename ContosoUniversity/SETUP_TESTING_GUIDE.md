# Setup and testing

## Prerequisites

- .NET 10 SDK
- SQL Server. The default LocalDB connection string requires Windows; set `ConnectionStrings:DefaultConnection` for another SQL Server environment.

## Build and run

From the `ContosoUniversity` directory:

```sh
dotnet restore
dotnet build
dotnet run
```

The application creates the database and seeds sample records on startup. No MSMQ setup is needed; notifications use a process-local channel and are transient.

## Smoke test

1. Open the application and navigate to Students, Courses, Instructors, or Departments.
2. Create or edit an entity and verify the operation completes.
3. Open Notifications and verify the entity-change notification appears.
4. Upload a JPG, JPEG, PNG, GIF, or BMP file under 5 MB while creating or editing a course. Verify that its image is shown in the course list/details.

There is no separate automated test project in this repository. The solution can be validated with `dotnet build ContosoUniversity.sln`; database-backed smoke tests require a reachable SQL Server.
