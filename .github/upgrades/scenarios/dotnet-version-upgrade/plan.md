# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Upgrade Options (confirmed)

| Option | Selection | Rationale |
|---|---|---|
| Target Framework | `net10.0` | LTS, requested by orchestrator |
| Project Approach | **In-place rewrite** | Single project, no test project, small codebase (~1,800 LOC); side-by-side overhead not justified |
| Project format | SDK-style (`Microsoft.NET.Sdk.Web`) | Required for net10.0 |
| Data access | Keep EF Core, bump `Microsoft.EntityFrameworkCore.SqlServer` 3.1.32 → 10.0.14 | Already EF Core, no EDMX/EF6 migration needed |
| SqlClient | Keep `Microsoft.Data.SqlClient`, bump 2.1.4 → 7.1.1 | Already modern client |
| Notification queue | Replace MSMQ (`System.Messaging`) with in-process `ConcurrentQueue<Notification>` wrapped in the same `NotificationService` API | MSMQ is Windows-only; no cross-platform equivalent; only consumer is in-repo JS polling the same process — see assessment.md Risk #1 |
| JSON endpoints wire-contract gate | **NOT APPLICABLE** (evidence recorded in assessment.md Risk #2) | No external consumer, no OData, no custom formatters |
| JSON casing | Preserve PascalCase via `JsonSerializerOptions.PropertyNamingPolicy = null` | Avoid breaking in-repo `notifications.js` which reads PascalCase fields |
| Views | Keep classic `HtmlHelper` syntax (compiles as-is in ASP.NET Core); only remove `@Scripts.Render`/`@Styles.Render` | Minimizes view diff; HtmlHelpers are still supported in ASP.NET Core |
| Bundling | Drop `System.Web.Optimization`; reference existing vendored CSS/JS directly from `wwwroot` | No build pipeline/tests depend on minified bundle output |
| Tests | None exist; none created (`generateNewUnitTests=false`) | Per task success criteria |

## Task Breakdown

Executed sequentially (in-place mode, single project):

1. **01-project-sdk-conversion** — Convert `ContosoUniversity.csproj` to SDK-style, target `net10.0`, remove `packages.config`, add modernized `PackageReference`s, remove web-specific legacy MSBuild imports/items.
2. **02-host-and-configuration** — Create `Program.cs` (minimal hosting model), migrate `Web.config` → `appsettings.json` (+ `appsettings.Development.json`), wire `IConfiguration`, register routing (`MapControllerRoute`), static files, DI container registrations (`SchoolContext`, `NotificationService` singleton/queue).
3. **03-data-and-services** — Port `Data/*` (SchoolContext, DbInitializer, factory → DI-based startup seeding) and `Services/NotificationService` (MSMQ → `ConcurrentQueue`), delete `LoggingService.cs` (empty/unused).
4. **04-controllers-and-models** — Port all controllers to `Microsoft.AspNetCore.Mvc`, fix `HttpPostedFileBase` → `IFormFile`, `Server.MapPath` → `IWebHostEnvironment`, `HttpNotFound()`/`HttpStatusCodeResult` → `NotFound()`/`StatusCode()`, move Models/PaginatedList unchanged.
5. **05-views-and-static-assets** — Remove `@Scripts.Render`/`@Styles.Render`, move `Content`/`Scripts`/`Uploads` under `wwwroot`, add `_ViewImports.cshtml`, verify `_ViewStart.cshtml`.
6. **06-cleanup-and-build-validation** — Remove `Global.asax(.cs)`, `App_Start/*`, `packages.config`, `Web.config`; build solution; fix all warnings; verify success criteria (build, tests, security/compliance).

## Success Criteria

- [ ] Project targets `net10.0`, SDK-style
- [ ] Builds with 0 errors, 0 warnings
- [ ] No `System.Web`, MSMQ/`System.Messaging`, or `packages.config` references remain
- [ ] All controllers/views functionally equivalent to baseline
- [ ] No test project exists → no tests to fail (`passUnitTests` trivially satisfied)
- [ ] No vulnerable/EOL package versions remain
