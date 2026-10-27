# Scenario Instructions — .NET Version Upgrade

## Summary

Upgrade `ContosoUniversity` from .NET Framework 4.8 (ASP.NET MVC 5, non-SDK-style) to .NET 10.0
(SDK-style, ASP.NET Core MVC).

## Preferences

### Flow Mode

- **Mode**: Automatic — do not pause for review at stage boundaries. Only pause if genuinely
  blocked (missing info, ambiguous decision with major consequences).

### Source Control

- Working branch: `upgrade/dotnet10-contosouniversity` (created from `main`, which was clean).
- Commit strategy: commit at logical checkpoints during execution.

### Environment Note

- This sandbox is Linux-only; no Visual Studio / `msbuild.exe` is available. The legacy
  `ContosoUniversity.csproj` imports `Microsoft.WebApplication.targets` (VS Web Application
  project type), which does not exist on this machine and blocks MSBuild evaluation (used by
  the assessment/conversion MCP tools). A no-op stub `Microsoft.WebApplication.targets` was
  placed under the current `dotnet` SDK's `Microsoft/VisualStudio/v18.0/WebApplications/` folder
  purely to unblock MSBuild *evaluation* for tooling. It is not part of the repo and is removed
  once the project is converted to SDK-style (which drops this import entirely).

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-once — single project in solution, no dependency graph to order.

### Project Structure
- Project Approach: In-place rewrite — user explicitly requested converting
  `ContosoUniversity.csproj` itself to net10.0 SDK-style (not side-by-side / not a new project).
- Package Management: N/A (single project, no CPM needed).

### Compatibility
- Unsupported Packages: Replace with net10.0-compatible equivalents (Microsoft.AspNet.Mvc,
  Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages, Microsoft.AspNet.Web.Optimization,
  Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Web.Infrastructure,
  NETStandard.Library are all obsolete under ASP.NET Core / net10.0 and will be removed; their
  functionality is replaced by ASP.NET Core MVC built-ins).
- System.Web Adapters: Not used — full rewrite to ASP.NET Core MVC was explicitly requested
  rather than a System.Web-compatibility-shim approach.
- Assembly Binding Redirects: Remove entirely — SDK-style/net10.0 projects do not use
  binding redirects (unify via PackageReference versions instead).

### Modernization
- Configuration Migration: Migrate `Web.config` `<appSettings>` and `<connectionStrings>` to
  `appsettings.json` + `IConfiguration`.
- Entity Framework: Upgrade Microsoft.EntityFrameworkCore 3.1.32 family → 10.0.x (net10-aligned).
- Nullable Reference Types: Leave disabled (not explicitly requested; avoid unrelated churn).

## Decisions

- Target framework: `net10.0` (confirmed by user request).
- Task id for tracking in this MCP workflow: `001-upgrade-dotnet-to-net10` (mirrors the
  externally supplied task id from the user's request).
