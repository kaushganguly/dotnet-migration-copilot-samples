# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Project: ContosoUniversity/ContosoUniversity.csproj
- Solution: ContosoUniversity/ContosoUniversity.sln
- Current target: .NET Framework 4.8 (legacy csproj, ASP.NET MVC web application)
- Target framework: net10.0
- Conversion: legacy csproj to SDK-style project format

## Success Criteria
- passBuild=true
- passUnitTests=true

## Preferences

### Flow Mode
Automatic - do not pause for user confirmation at any stage. Only stop if genuinely blocked
(missing info, ambiguous decision with major consequences, or unrecoverable error).

### Source Control
- Git repo: yes
- Working branch: upgrade/dotnet10-contosouniversity (created from main, clean tree at start)
- Commit strategy: commit at logical checkpoints (not required to pause for review)

### Technical Preferences
- Target framework: net10.0 (explicitly requested by user)
- Accept all tool defaults for package versions, migration strategy, etc.

## Environment Notes
- Environment is Linux (no Visual Studio). The legacy csproj imports
  $(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets which does not exist in this
  environment - get_dotnet_upgrade_options fails for this reason on this project. Proceeding
  using convert_project_to_sdk_style directly, which removes this import as part of SDK-style
  conversion.

## Decisions
- Proceeding directly to SDK-style conversion + target framework retarget to net10.0 (ASP.NET MVC
  legacy web app -> ASP.NET Core style SDK project), since get_dotnet_upgrade_options cannot run
  due to missing Visual Studio web application build targets in this Linux CI environment.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects
- Nullable Reference Types: Leave Disabled

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project in the solution (ContosoUniversity.csproj), no dependency graph to manage.

### Execution Constraints
- Single atomic upgrade task covers: SDK-style conversion + TFM retarget + package updates + API fixes (merged per "very small solutions (1-2 projects)" adaptation rule)
- One bounded pass: update project file, update packages, restore, build and fix all compilation errors
- Full solution build + test validation after the atomic upgrade completes
- Project Approach = In-place rewrite: no side-by-side scaffold/migrate tasks, no YARP proxy, web project is upgraded directly
- System.Web Adapters = Direct Migration: replace System.Web APIs with ASP.NET Core native equivalents directly (no compatibility shim package)
- Unsupported Packages = Resolve Inline: Antlr -> Antlr4 (known replacement), System.Web.Optimization bundling -> replace with static script/link tags (Feature.0001)
- Assembly Binding Redirects = Remove: binding redirects dropped as part of SDK-style/PackageReference conversion
- Microsoft.Data.SqlClient 2.1.4 has a known security vulnerability -> upgrade to 7.1.1 as part of package updates
