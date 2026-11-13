# Contoso University - ASP.NET Core 10.0

This project is an ASP.NET Core MVC application targeting .NET 10.0 (migrated from ASP.NET MVC 5 on .NET Framework 4.8.2).

## Project Overview

### Framework
- ASP.NET Core MVC (.NET 10.0), SDK-style project

### Database Access: Entity Framework
- Entity Framework Core 10.0.15

### Project Structure
```
ContosoUniversity/
├── Controllers/              # MVC Controllers
├── Data/                     # Entity Framework context and initializer
├── Models/                   # Data models and view models
├── Services/                 # Notification service (MSMQ.Messaging)
├── Views/                    # Razor views (+ _ViewImports.cshtml)
├── wwwroot/                   # Static assets (Content/, Scripts/, Uploads/)
├── Program.cs                 # Minimal-hosting entry point (replaces Global.asax/App_Start)
├── appsettings.json            # Configuration (replaces Web.config)
└── ContosoUniversity.csproj    # SDK-style project file
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
   - Microsoft Message Queue (MSMQ) Server enabled (Windows-only; required for the notification feature)

2. **Setup**:
   ```
   dotnet restore
   dotnet build
   dotnet run
   ```

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
