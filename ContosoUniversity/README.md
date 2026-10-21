# Contoso University - .NET 10.0

This project is an ASP.NET Core MVC application targeting .NET 10.0. It was migrated from
ASP.NET MVC 5 (.NET Framework 4.8) via an in-place rewrite - see
`.github/upgrades/scenarios/dotnet-version-upgrade/` for the full migration plan and notes.

## Project Overview

### Framework
- ASP.NET Core MVC (.NET 10.0), SDK-style project (`Microsoft.NET.Sdk.Web`)

### Database Access: Entity Framework
- Entity Framework Core 10.0.13

### Project Structure
```
ContosoUniversity/
├── Controllers/             # MVC Controllers
├── Data/                    # Entity Framework context and initializer
├── Models/                  # Data models and view models
├── Views/                   # Razor views
├── Services/                # Notification service (MSMQ) and options
├── wwwroot/                  # Static assets (CSS, JS, uploads) served by ASP.NET Core
├── Program.cs                # Application startup/hosting (replaces Global.asax/App_Start)
├── appsettings.json          # Configuration (replaces Web.config)
└── ContosoUniversity.csproj  # SDK-style project file (replaces packages.config)
```

## Database Configuration

The application uses SQL Server LocalDB with the following connection string in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=ContosoUniversityNoAuthEFCore;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
}
```

## Running the Application

1. **Prerequisites**:
   - .NET 10.0 SDK
   - SQL Server LocalDB (Windows) or another SQL Server instance reachable via the connection string
   - Microsoft Message Queue (MSMQ) Server enabled (Windows-only; required for the notification feature)

2. **Setup**:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run` (or `dotnet watch run` for development)

> **Note**: LocalDB and MSMQ are Windows-only facilities. On non-Windows environments
> (e.g. Linux CI/sandboxes) the app still builds and serves static-content/no-DB routes,
> but any request that touches `SchoolContext` or `NotificationService` will fail until a
> reachable SQL Server instance and a real MSMQ broker are available. This is a pre-existing
> environment requirement of the application, unchanged by the .NET 10 migration.

## Features

- **Student Management**: CRUD operations for students with pagination and search
- **Course Management**: Manage courses and their assignments to departments
- **Instructor Management**: Handle instructor assignments and office locations
- **Department Management**: Manage departments and their administrators
- **Statistics**: View enrollment statistics by date

## Database Initialization

The application uses Entity Framework Core Code First with a database initializer that:
- Creates the database if it doesn't exist
- Seeds sample data including students, instructors, courses, and departments
- Handles model changes by recreating the database
