# Contoso University

Contoso University is an ASP.NET Core MVC application targeting .NET 10. It uses Entity Framework Core 10 with SQL Server.

## Project structure

```text
ContosoUniversity/
├── Controllers/       # MVC controllers
├── Data/              # EF Core context and database initializer
├── Models/            # Entity and view models
├── Services/          # Application services
├── Views/             # Razor views
├── Content/           # CSS assets served at /Content
├── Scripts/           # JavaScript assets served at /Scripts
├── Uploads/           # Course teaching-material images
├── Program.cs         # Hosting, dependency injection, middleware, and routes
└── appsettings.json   # Connection string and logging configuration
```

## Prerequisites

- .NET 10 SDK
- SQL Server (the default connection string uses SQL Server LocalDB on Windows)

Set `ConnectionStrings:DefaultConnection` in `appsettings.json` or an environment-specific configuration source to your SQL Server connection string. The app creates and seeds its database at startup.

## Build and run

From this directory:

```sh
dotnet restore
dotnet build
dotnet run
```

## Features

- Student management with search, sorting, and pagination
- Course management and department assignments
- Instructor assignments and office locations
- Department management and enrollment statistics
- Persisted notifications for create, update, and delete operations
- Course teaching-material image uploads
