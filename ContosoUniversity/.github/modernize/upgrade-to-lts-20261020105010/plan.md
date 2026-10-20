# .NET Upgrade Plan — ContosoUniversity

## Summary

| | |
|---|---|
| **Source .NET version** | .NET Framework 4.8 |
| **Target .NET version** | .NET 10.0 (net10.0) — latest LTS |
| **Solution type** | Single project (ASP.NET MVC 5 web application) |
| **Project** | ContosoUniversity.csproj |

## Projects in Solution

- **ContosoUniversity** (`ContosoUniversity.csproj`) — ASP.NET MVC 5 web application targeting `.NET Framework v4.8`, using non-SDK-style (legacy) project format.

## Current State Analysis

- Legacy (non-SDK-style) `.csproj` using `ToolsVersion="15.0"` and `Microsoft.WebApplication.targets`.
- ASP.NET MVC 5 (`System.Web.Mvc`, `System.Web.Razor`, `System.Web.Optimization`) built on `System.Web` (IIS/System.Web pipeline).
- Data access via EF Core 3.1 (`Microsoft.EntityFrameworkCore.SqlServer` 3.1.32) running on .NET Framework — EF Core 3.1 is end-of-life.
- `packages.config`-based NuGet dependency management.
- Dependencies include `Microsoft.Data.SqlClient`, `Microsoft.Identity.Client`, `Newtonsoft.Json`, `WebGrease`, `Microsoft.AspNet.WebPages`, `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`.
- Views (`.cshtml`) use classic ASP.NET MVC 5 Razor syntax and `Views\Web.config` based view engine configuration.
- `Global.asax` / `Global.asax.cs` application startup model.

## Why Upgrade

.NET Framework 4.8 is the final version of .NET Framework and receives only security fixes; it does not receive new features, performance improvements, or full compatibility with modern Azure SDKs and tooling. The project also depends on EF Core 3.1, which is out of support. Moving to .NET 10 (LTS) provides long-term support, improved performance, cross-platform hosting, modern ASP.NET Core APIs, and current Azure SDK/EF Core compatibility.

## High-Level Upgrade Scope

1. **Project file modernization** — Convert the legacy `ContosoUniversity.csproj` to the SDK-style format (`Microsoft.NET.Sdk.Web`) targeting `net10.0`.
2. **Framework/runtime migration** — Replace `System.Web`-based ASP.NET MVC 5 pipeline with ASP.NET Core MVC (controllers, routing, middleware, `Program.cs` hosting model) to replace `Global.asax`.
3. **NuGet dependency updates** — Replace `packages.config` with PackageReference, update/replace packages with .NET 10–compatible equivalents (e.g., `Microsoft.EntityFrameworkCore.SqlServer` to a current EF Core version, `Microsoft.Data.SqlClient`, `Microsoft.Identity.Client`, `Newtonsoft.Json` or migrate to `System.Text.Json`, remove legacy packages such as `WebGrease`/`Microsoft.AspNet.WebPages`/`Microsoft.AspNet.Mvc`/`Microsoft.AspNet.Razor` in favor of ASP.NET Core bundling/minification and Razor views).
4. **API/code compatibility fixes** — Update controllers, views, data access (`SchoolContext`, `DbInitializer`, `SchoolContextFactory`), services, and models for ASP.NET Core and modern EF Core APIs; address any `System.Web`-specific API usage (e.g., `HttpContext`, routing, bundling, filters).
5. **Configuration migration** — Migrate `Web.config`/`Web.Debug.config`/`Web.Release.config` settings to `appsettings.json` and the ASP.NET Core configuration system.
6. **Validation** — Ensure the project builds successfully and existing unit tests pass on .NET 10.

The specific implementation steps (exact package versions, code transformations, and file-by-file changes) will be determined by the upgrade execution agent.
