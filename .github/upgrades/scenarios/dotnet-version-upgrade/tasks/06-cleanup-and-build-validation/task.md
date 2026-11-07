# 06-cleanup-and-build-validation

## Objective
Delete the now-dead legacy hosting files (`Global.asax(.cs)`, `App_Start/*.cs`, `Web.config`),
run a full clean build with zero errors/zero warnings, grep-verify no stray
`System.Web`/`System.Messaging` references remain, and verify all 4 success criteria.

## Research Findings
- Confirmed remaining build errors (22 total, from task 05's build) are **entirely** confined to:
  `App_Start/BundleConfig.cs` (4), `App_Start/FilterConfig.cs` (4), `App_Start/RouteConfig.cs` (6),
  `Global.asax.cs` (8) — all dead code since `Program.cs` (task 02) already replaces every piece
  of their functionality (routing, filters ported — `FilterConfig` only registered
  `HandleErrorAttribute`, which has no Core equivalent/need since `UseExceptionHandler` + MVC's
  built-in model validation already cover the equivalent cases; bundles replaced by direct
  `<link>/<script>` tags in task 05; DB init ported to `Program.cs`).
  Confirmed via `ls`: no `Web.Debug.config`/`Web.Release.config` exist in this repo (nothing to
  delete there beyond `Web.config` itself).
- Confirmed `ContosoUniversity.sln` only references the project by relative path + GUID — no
  changes needed (SDK-style project with a different internal structure doesn't invalidate the
  `.sln` project reference).
- No other `.cs`/`.cshtml`/project files reference `Global.asax`, `App_Start`, or
  `System.Web`/`System.Messaging`/`ConfigurationManager` (to be grep-verified as the final gate
  below) outside the files already migrated in tasks 01-05.

## Steps
1. Delete `Global.asax`, `Global.asax.cs`, `App_Start/BundleConfig.cs`,
   `App_Start/FilterConfig.cs`, `App_Start/RouteConfig.cs`, `Web.config`.
2. Remove the now-empty `App_Start/` directory.
3. `dotnet build` (clean, `-warnaserror` style check) — require 0 errors, 0 warnings.
4. Grep-verify repo-wide: no `System.Web` (excluding comments/docs), no `System.Messaging`, no
   `ConfigurationManager`, no `HttpPostedFileBase`, no `Scripts.Render`/`Styles.Render` remain in
   any `.cs`/`.cshtml` file.
5. `dotnet restore` security check — confirm 0 NuGet vulnerability advisories (NU1902/NU1903).
6. No test project exists in the solution — `passUnitTests` is vacuously satisfied (nothing to
   run); confirmed via `discover_test_projects`-equivalent manual search (no `*.Tests.csproj`/
   `*Test*.csproj` anywhere in the repo).
7. Write final summary covering all 4 success criteria; report back to the user per the original
   task instructions (finalStatus, successCriteriaStatus, summary of changes).

## Files Modified
- Global.asax (deleted)
- Global.asax.cs (deleted)
- App_Start/BundleConfig.cs (deleted)
- App_Start/FilterConfig.cs (deleted)
- App_Start/RouteConfig.cs (deleted)
- Web.config (deleted)
