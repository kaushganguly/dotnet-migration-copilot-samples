# .NET Upgrade Plan — ContosoUniversity

## Summary

| Item | Value |
|------|-------|
| Source framework | .NET Framework 4.8 |
| Target framework | .NET 10.0 (LTS) |
| Project type | ASP.NET MVC 5 web application (non-SDK-style project) |
| Solution | ContosoUniversity.sln |
| Projects in scope | ContosoUniversity.csproj (ContosoUniversity) |

## Why upgrade is needed

The project currently targets .NET Framework 4.8, which is a legacy, non-SDK-style
project that cannot consume modern Azure SDKs, modern NuGet packages (net standard 2.0+/net6.0+ only),
or participate in the regular .NET LTS support cadence. .NET Framework receives
only security fixes and no feature or performance investment, and does not align
with Azure App Service's recommended modern runtimes. Upgrading to **.NET 10.0** (the
current LTS release) brings the application onto a fully supported, actively
developed platform with improved performance, cross-platform hosting, container
support, and continued long-term servicing.

## Scope of the upgrade

1. **Project file modernization**: Convert `ContosoUniversity.csproj` from the
   legacy XML project format to the modern SDK-style (`Microsoft.NET.Sdk.Web`)
   format, and update `<TargetFramework>` from `v4.8` to `net10.0`.
2. **Application framework migration**: Migrate the ASP.NET MVC 5 (`System.Web.Mvc`,
   `System.Web.Routing`, `Global.asax`) pipeline to ASP.NET Core MVC (controllers,
   views, routing, startup/hosting model, `Program.cs`).
3. **Dependency updates**: Replace/upgrade NuGet packages that are incompatible
   with .NET 10 (e.g., `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.WebPages`,
   `Microsoft.AspNet.Web.Optimization`, `WebGrease`, `Antlr`) with their ASP.NET
   Core / modern equivalents, and update packages already partially modern
   (`Microsoft.EntityFrameworkCore*` 3.1.x → EF Core 10.x, `Microsoft.Data.SqlClient`,
   `Microsoft.Extensions.*`, `Microsoft.Identity.Client`, `Newtonsoft.Json`) to
   versions compatible with .NET 10.
4. **Configuration migration**: Migrate `Web.config` app/connection settings to
   `appsettings.json` and the .NET configuration system; replace
   `Web.Debug.config`/`Web.Release.config` transforms with environment-based
   configuration.
5. **API compatibility fixes**: Address breaking changes in EF Core, routing,
   bundling/minification (`System.Web.Optimization` → modern static asset
   pipeline), and any other APIs not available on .NET 10.
6. **Build and packaging updates**: Update build/publish settings for the new
   SDK-style project (`web.config` IIS integration or self-contained hosting),
   remove `packages.config` in favor of `PackageReference`.

## Out of scope

- No containerization, deployment, or infrastructure provisioning is included
  in this plan (not requested).
- No new features or unrelated refactoring.

## Execution

This plan defines a single upgrade task (see `.metadata/tasks.json`) that will
be executed by the modernization upgrade engineer, which determines the exact
implementation steps.
