# Scenario Instructions — dotnet-version-upgrade

## Scope
- Project: ContosoUniversity.csproj (ASP.NET MVC 5, .NET Framework 4.8)
- Target Framework: net10.0
- Task: 001-upgrade-dotnet-to-net10

## Preferences

### Flow Mode
Automatic — do not pause for user review at any stage. Only pause on hard blockers.

### Technical Preferences
- Target framework: net10.0
- Convert to SDK-style project format
- Migrate from ASP.NET MVC 5 (System.Web) to ASP.NET Core MVC
- Update NuGet dependencies to .NET 10 compatible versions
- Migrate configuration (Web.config -> appsettings.json / Program.cs)
- Fix API compatibility issues across controllers, data access, services, and views

### Execution Style
- Fully autonomous, end-to-end execution
- generateNewUnitTests=false - do not author new unit tests unless required to validate a fix
- Must pass build and existing unit tests
- Must pass security compliance check (no vulnerable packages)

## Decisions
- Source control: created working branch `upgrade/001-upgrade-dotnet-to-net10` from `main` before initializing scenario.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects); System.Web Libraries: In-place (fixed)

### Compatibility
- Unsupported Packages: Resolve Inline (1 package without known replacement: Microsoft.AspNet.Web.Optimization)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects
- Nullable Reference Types: Enable Nullable Reference Types
