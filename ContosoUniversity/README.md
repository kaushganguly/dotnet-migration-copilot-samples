# Contoso University — .NET 10

This project is an ASP.NET Core MVC application targeting .NET 10.

## Project Overview

### Framework
- ASP.NET Core MVC (.NET 10)

### Database Access: Entity Framework
- Entity Framework Core 10

### Project Structure
```
ContosoUniversity/
├── Program.cs              # Application startup and service configuration
├── Controllers/            # MVC Controllers
├── Data/                   # Entity Framework context and initializer
├── Models/                 # Data models and view models
├── Views/                  # Razor views
├── wwwroot/                # Static assets and uploaded teaching materials
├── appsettings.json        # Application and connection configuration
└── ContosoUniversity.csproj
```

## Database Configuration

The application uses SQL Server LocalDB by default. Configure `ConnectionStrings__DefaultConnection`
to override the connection string for another environment.

## Running the Application

1. **Prerequisites**:
   - .NET 10 SDK
   - SQL Server LocalDB

2. **Build and run** from the `ContosoUniversity` directory:
   ```sh
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
