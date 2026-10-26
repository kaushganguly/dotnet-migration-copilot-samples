# Upgrade Plan: ContosoUniversity → .NET 10.0

## Summary

- **Source**: .NET Framework 4.8, legacy-style csproj, ASP.NET MVC 5, `packages.config`
- **Target**: .NET 10.0 (LTS), SDK-style csproj (`Microsoft.NET.Sdk.Web`), ASP.NET Core MVC, `PackageReference`
- **Mode**: In-place rewrite (single project, no side-by-side host — project approach: In-place)
- **Scope**: `ContosoUniversity/ContosoUniversity.csproj` (only project in the solution)

## Upgrade Options (confirmed — Automatic mode, no external consumers of JSON endpoint)

- **Project Approach**: In-place rewrite (small single-project app; no requirement to keep the
  Framework host running side-by-side).
- **Package Management**: Convert `packages.config` → `PackageReference`, update to .NET 10-compatible
  versions (EF Core 3.1.x → 10.0.x, Microsoft.Data.SqlClient 2.1.4 → 7.1.x, Newtonsoft.Json 13.0.3 → 13.0.4,
  drop MVC5/WebPages/Razor/Optimization/WebGrease/Antlr packages no longer needed, add `MSMQ.Messaging`
  replacing `System.Messaging`).
- **JSON wire compatibility**: `NotificationsController` JSON endpoints are consumed only by this app's
  own `notifications.js` (not an externally published API) — preserve existing PascalCase property
  casing via `JsonSerializerOptions.PropertyNamingPolicy = null` rather than changing the JS client.

## Task Breakdown

1. **01-sdk-style-and-packages** — Convert project file to SDK-style (`Microsoft.NET.Sdk.Web`),
   target `net10.0`, migrate `packages.config` → `PackageReference` with updated versions, remove
   `packages.config`/`packages/` and legacy web-application MSBuild imports.
2. **02-aspnetcore-migration** — Migrate ASP.NET MVC 5 → ASP.NET Core MVC: `Program.cs` host,
   `appsettings.json` configuration (from `Web.config`), DI registrations (`SchoolContext`,
   `NotificationService`), controllers (`System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`), routing
   (`RouteConfig` → `MapControllerRoute`), Razor views (remove bundling, move static assets to
   `wwwroot/`), MSMQ migration (`System.Messaging` → `MSMQ.Messaging`), cleanup of `App_Start/`,
   `Global.asax(.cs)`, `Views/Web.config`.
3. **03-build-validate** — Restore, build, fix compilation/warnings, smoke-verify project structure,
   confirm no `System.Web`/`System.Messaging` references remain, record results.

## Success Criteria

- [ ] Project targets `net10.0`, SDK-style, `PackageReference`-based
- [ ] No `System.Web.*` or legacy ASP.NET MVC 5 package references remain
- [ ] Solution builds with 0 errors (warnings fixed in modified project)
- [ ] No unit test projects exist in the repo — `passUnitTests` trivially satisfied
- [ ] No known security vulnerabilities in updated package set
