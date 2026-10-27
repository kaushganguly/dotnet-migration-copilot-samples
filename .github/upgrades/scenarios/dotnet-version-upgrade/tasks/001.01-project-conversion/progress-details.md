# Progress — 001.01-project-conversion

## Changes
- Replaced `ContosoUniversity.csproj` with SDK-style (`Microsoft.NET.Sdk.Web`), `TargetFramework=net10.0`.
- Removed `packages.config` and `Properties/AssemblyInfo.cs` (empty `Properties/` folder removed).
- Package set: `Microsoft.EntityFrameworkCore`/`.SqlServer`/`.Design`/`.Tools` 10.0.14,
  `Microsoft.Data.SqlClient` 7.1.1, `Newtonsoft.Json` 13.0.4, plus an explicit
  `System.Security.Cryptography.Pkcs` 10.0.14 pin to patch a moderate/high severity
  vulnerability (GHSA-2fmv-rv2c-59x7 / GHSA-846f-f764-76mx) in the 9.0.18 version pulled in
  transitively by `Microsoft.Data.SqlClient`.
- Dropped all legacy references, HintPaths, `CopySQLClientNativeBinaries` custom target, the
  `Microsoft.WebApplication.targets` import, binding redirects, and explicit `<Compile>`/
  `<Content>` item lists (SDK-style default globbing covers them; items actually relocated to
  `wwwroot/` are handled in task 001.02).

## Validation
- `dotnet restore ContosoUniversity.csproj` — succeeded, 0 warnings, 0 errors.
- Full `dotnet build` deferred to task 001.05 (code still references `System.Web.*` until tasks
  001.02–001.04 land; an intermediate build at this point would show expected errors for files
  not yet migrated).
