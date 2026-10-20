# Contoso University - .NET 10

Contoso University is an ASP.NET Core MVC application targeting .NET 10. It uses Entity Framework Core with SQL Server.

## Project structure

```text
ContosoUniversity/
├── Controllers/      # ASP.NET Core MVC controllers
├── Data/             # EF Core context and sample-data initializer
├── Models/           # Data and view models
├── Services/         # Application services
├── Views/            # Razor views
├── wwwroot/          # Static assets and teaching-material uploads
├── Program.cs        # Hosting, dependency injection, routing, and startup
└── appsettings.json  # Connection string and logging configuration
```

## Requirements and configuration

- .NET 10 SDK
- SQL Server; the default connection string uses SQL Server LocalDB on Windows
- Set `ConnectionStrings:DefaultConnection` in `appsettings.json` or through configuration/environment variables for your SQL Server instance

The application initializes and seeds the database at startup. Uploaded teaching-material images are stored under `wwwroot/Uploads/TeachingMaterials`; the application creates the directory when needed.

## Build, test, and run

From this directory:

```sh
dotnet restore ContosoUniversity.sln
dotnet test ContosoUniversity.sln
dotnet run --project ContosoUniversity/ContosoUniversity.csproj
```

## Features

- Student, course, instructor, and department management
- Enrollment statistics and paginated student search
- Course teaching-material image uploads
- Database-backed notifications for entity create, update, and delete operations
