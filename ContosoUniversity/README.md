# Contoso University - .NET 10

This project is an ASP.NET Core MVC application targeting .NET 10.0. It was migrated
from ASP.NET MVC 5 on .NET Framework 4.8 via an in-place rewrite (SDK-style project,
minimal hosting `Program.cs`, `Microsoft.AspNetCore.Mvc` controllers, and a
cross-platform in-process notification queue replacing MSMQ).

## Project Overview

### Framework
- ASP.NET Core MVC (.NET 10.0)

### Database Access: Entity Framework
- Entity Framework Core 10.0

### Project Structure
```
ContosoUniversity/
├── Controllers/            # MVC Controllers
├── Data/                   # Entity Framework context and initializer
├── Models/                 # Data models and view models
├── Views/                  # Razor views
├── wwwroot/                # Static assets (css, js, uploaded teaching materials)
├── Program.cs              # Minimal hosting entry point, DI, middleware, routing
├── appsettings.json         # Configuration (connection strings, logging)
└── ContosoUniversity.csproj # SDK-style project file
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
   - SQL Server LocalDB (Windows) or any SQL Server reachable via the connection string above

2. **Setup**:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run` (or `dotnet watch run` for development)

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
