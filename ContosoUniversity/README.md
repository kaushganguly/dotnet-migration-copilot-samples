# Contoso University

Contoso University is an ASP.NET Core MVC application targeting .NET 10.0.

## Stack

- ASP.NET Core MVC with SDK-style project format
- Entity Framework Core 10 and SQL Server
- PackageReference-based NuGet dependencies
- JSON APIs using the ASP.NET Core `System.Text.Json` defaults

## Project structure

```text
ContosoUniversity/
├── Controllers/       # MVC controllers
├── Data/              # EF Core context and database initializer
├── Models/            # Entity and view models
├── Services/          # Application services
├── Views/             # ASP.NET Core Razor views
├── wwwroot/           # CSS, JavaScript, and uploaded teaching material images
├── Program.cs         # Hosting, dependency injection, and routing
└── appsettings.json   # Application configuration
```

## Database configuration

The default connection string targets SQL Server LocalDB and is configured in
`appsettings.json`. Override it for another environment with the standard
`ConnectionStrings__DefaultConnection` environment variable. The database is
created and seeded on application startup.

## Run

Install the .NET 10 SDK and configure a reachable SQL Server instance, then run:

```sh
dotnet restore ContosoUniversity.sln
dotnet run --project ContosoUniversity.csproj
```

Course image uploads are limited to 5 MB, and the total multipart request limit
is 10 MB. Uploaded images are stored under `wwwroot/Uploads/TeachingMaterials`.
