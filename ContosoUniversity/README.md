# Contoso University

Contoso University is an ASP.NET Core MVC application targeting .NET 10. It uses Entity Framework Core 10 with SQL Server.

## Run the application

1. Install the .NET 10 SDK and configure a reachable SQL Server in `ConnectionStrings:DefaultConnection` in `appsettings.json` or through configuration/environment variables.
2. From this directory, run `dotnet restore`, `dotnet build`, and `dotnet run`.
3. On startup, the application creates the database and seeds sample data when the database is empty.

The checked-in connection string uses SQL Server LocalDB. LocalDB is available on Windows; use an appropriate SQL Server connection string on other platforms.

## Project structure

- `Program.cs`: ASP.NET Core services, middleware, routes, and database initialization.
- `Controllers/`, `Models/`, `Data/`, and `Views/`: MVC application and EF Core data access.
- `wwwroot/`: static assets and uploaded teaching-material images.

## Notifications and uploads

Entity-change notifications use a bounded, in-process channel instead of the Windows-only MSMQ API. Notifications are transient and are lost when the application restarts. Teaching-material images are stored below `wwwroot/Uploads/TeachingMaterials/`; accepted formats and the 5 MB per-image limit are enforced by the upload controller.
