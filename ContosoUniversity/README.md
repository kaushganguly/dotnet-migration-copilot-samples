# Contoso University - .NET 10.0

This project is an ASP.NET Core MVC application targeting .NET 10.0.

> **Migration note**: This project was upgraded from ASP.NET MVC 5 / .NET Framework 4.8 to ASP.NET Core MVC / .NET 10.0, including conversion to an SDK-style project, migration off `System.Web`, NuGet dependency updates, and configuration migration from `Web.config` to the ASP.NET Core configuration system (`appsettings.json`). See `.github/upgrades/dotnet-version-upgrade/` for the full assessment and migration plan.

## Project Overview

### Framework
- ASP.NET Core MVC (.NET 10.0)

### Database Access: Entity Framework
- Entity Framework Core 10.0.15

### Project Structure
```
ContosoUniversity/
├── Controllers/            # MVC Controllers
├── Data/                   # Entity Framework context and initializer
├── Models/                 # Data models and view models
├── Views/                  # Razor views
├── wwwroot/                # Static web assets (CSS, JavaScript, uploads)
├── Properties/             # Launch/assembly properties
├── Program.cs              # Application entry point and host configuration
├── appsettings.json         # Configuration file
└── ContosoUniversity.csproj # SDK-style project file (NuGet via PackageReference)
```

## Database Configuration

The application uses SQL Server LocalDB with the following connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=ContosoUniversityNoAuthEFCore;Integrated Security=True;MultipleActiveResultSets=True"
  }
}
```

## Running the Application

1. **Prerequisites**:
   - .NET 10.0 SDK
   - SQL Server LocalDB (Windows) or a reachable SQL Server instance

2. **Setup**:
   - Restore NuGet packages: `dotnet restore`
   - Build the solution: `dotnet build`
   - Run: `dotnet run` (or `dotnet watch run` for development)

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
