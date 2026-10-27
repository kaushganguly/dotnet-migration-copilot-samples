# Progress — 001.05-build-and-test-validation

## Changes
- Fixed two issues surfaced by the first `dotnet build`:
  - `Program.cs`: added missing `using Microsoft.AspNetCore.Builder;` (+ `Microsoft.AspNetCore.Hosting`,
    `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`,
    `Microsoft.Extensions.Hosting`) — required because `ImplicitUsings` is disabled in the
    SDK-style csproj (CS0103: 'WebApplication' does not exist).
  - `HomeController.Unauthorized()`: added the `new` keyword (CS0114 warning) since it hides
    `ControllerBase.Unauthorized()` from the ASP.NET Core base class — this is a pre-existing
    custom action unrelated to the framework's built-in helper, so `new` preserves the
    existing action/route behavior without introducing a behavioral change.
- Removed the temporary `Microsoft.WebApplication.targets` stub file that had been placed
  under the .NET SDK installation path (`/usr/share/dotnet/sdk/10.0.403/Microsoft/VisualStudio/...`,
  **outside the repository**) during assessment to work around a Linux sandbox limitation
  (no Visual Studio / MSBuild web targets available) that blocked the legacy project's MSBuild
  evaluation. Verified `dotnet build` still succeeds without it now that the project is
  SDK-style (no longer imports that file).
- Updated `ContosoUniversity/.gitignore`: `Uploads/TeachingMaterials/*` → `wwwroot/Uploads/TeachingMaterials/*`
  (and the corresponding `!...gitkeep` negation) to match the new `wwwroot/Uploads` location
  from task 001.02; dropped the now-nonexistent `Uploads/web.config` ignore line.

## Validation
- `dotnet build ContosoUniversity.csproj` → **Build succeeded. 0 Warning(s). 0 Error(s).**
  (re-verified after removing the SDK targets stub — same clean result).
- Attempted a runtime smoke test: `dotnet run --no-build --urls http://127.0.0.1:5087`.
  The app started, DI container built successfully, but `DbInitializer.Initialize` crashed
  with `PlatformNotSupportedException: LocalDB is not supported on this platform` — this
  sandbox is Linux with no SQL Server/LocalDB instance available. This is an **environment
  limitation, not a migration regression**: the original ASP.NET Framework app also required
  LocalDB (Windows-only) and could not have run in this same Linux sandbox either. No code
  change was made to work around this (would have altered startup behavior beyond the scope
  of the requested migration); it is documented here as a known gap in this sandbox's
  verification coverage.
- **No test project exists anywhere in the repository or solution** (confirmed: solution has
  exactly one project, `ContosoUniversity.csproj`). The `passUnitTests=true` success
  criterion is therefore satisfied vacuously — there are no unit tests to run or fail.
- Committed all migration changes on the `upgrade/dotnet10-contosouniversity` branch.

## Success Criteria
- `passBuild=true` → **met** (clean `dotnet build`, 0 errors/0 warnings).
- `passUnitTests=true` → **met vacuously** (no test project in the repository).
