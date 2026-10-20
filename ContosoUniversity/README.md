# Contoso University - .NET 10

This project is an ASP.NET Core MVC application targeting .NET 10.

## Project Overview

### Framework
- ASP.NET Core MVC (.NET 10)

### Database Access: Entity Framework
- Entity Framework Core 10

### Project Structure
```
ContosoUniversity/
├── Program.cs              # ASP.NET Core startup and middleware
├── Controllers/            # MVC Controllers
├── Data/                   # Entity Framework context and initializer
├── Models/                 # Data models and view models
├── Views/                  # Razor views
├── Content/                # CSS and other content
├── Scripts/                # JavaScript files
├── appsettings.json        # Connection strings and application settings
└── ContosoUniversity.csproj # SDK-style project and NuGet dependencies
```

## Database Configuration

The default connection string is configured in `appsettings.json` for SQL Server LocalDB. Override it with the `ConnectionStrings__DefaultConnection` environment variable when using another SQL Server:
```sh
ConnectionStrings__DefaultConnection="Server=localhost;Database=ContosoUniversity;Integrated Security=True;TrustServerCertificate=True" dotnet run
```

## Running the Application

1. **Prerequisites**:
   - .NET 10 SDK
   - SQL Server LocalDB

2. **Setup**:
   - Run `dotnet restore ContosoUniversity.sln`
   - Run `dotnet build ContosoUniversity.sln`
   - Set `ConnectionStrings__DefaultConnection` for your SQL Server, if needed
   - Run `dotnet run --project ContosoUniversity.csproj`

## Features

- **Student Management**: CRUD operations for students with pagination and search
- **Course Management**: Manage courses and their assignments to departments
- **Instructor Management**: Handle instructor assignments and office locations
- **Department Management**: Manage departments and their administrators
- **Statistics**: View enrollment statistics by date

## Database Initialization

The application uses Entity Framework Core with a database initializer that:
- Creates the database if it doesn't exist
- Seeds sample data including students, instructors, courses, and departments
- Handles model changes by recreating the database
