# Scenario Instructions — dotnet-version-upgrade

## Scope
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj
- Source: .NET Framework 4.8 (legacy csproj, ASP.NET MVC / System.Web)
- Target: net10.0 (SDK-style project, ASP.NET Core)

## User Preferences

### Flow Mode
**Automatic** — Do not pause for user review at any stage (assessment, planning, breakdown, or
between tasks). Surface artifact paths inline and keep moving. Only pause if genuinely blocked
(missing info, ambiguous ownership, destructive irreversible action with no safe default).

### Technical Preferences
- Target framework: net10.0 (explicitly requested by user — do not substitute)
- Convert to SDK-style project format as part of this upgrade
- Migrate ASP.NET MVC (System.Web) APIs/configuration to ASP.NET Core equivalents
- Migrate NuGet packages (packages.config -> PackageReference) and update to compatible versions
- generateNewUnitTests: false — do not author new unit test projects/tests
- passUnitTests: true — any existing tests must pass after migration
- securityComplianceCheck: true — resolve known vulnerable package versions during migration

## Source Control
- Working branch: upgrade/dotnet10-contosouniversity (created from main, clean tree at start)
- Commit strategy: commit at meaningful checkpoints (post-assessment artifacts, post-plan, after
  each completed task) using the engine's standard task-execution flow.

## Decisions
- No existing scenario found on disk; started fresh initialization.
- Linux execution environment: no Visual Studio/MSBuild Web Application targets available —
  expect this project (Web Application project type) needs SDK-style conversion to build with
  `dotnet build` on this host; legacy project format reliant on VS-only targets will fail here.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project in the solution — no dependency graph to manage. |
| Project Approach (Web Projects) | In-place rewrite | Small web project (6 controllers, ~2k LOC), low assessment difficulty, no continuous-deployment constraint — side-by-side overhead not warranted. |
| Unsupported Packages | Resolve Inline | Small number of incompatible packages (Microsoft.AspNet.Web.Optimization), with a known replacement pattern (static asset references) — resolved within the main task. |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach confirmed; low/isolated HttpContext.Current usage expected in a small MVC app — no compatibility shim needed. |
| Assembly Binding Redirects | Remove Binding Redirects | All 12 flagged redirects are auto-generated/boilerplate MSB3836 conflicts against the same package set being upgraded; SDK-style conversion auto-manages redirects going forward. |

Not applicable (and not evaluated further): Package Management/CPM (single project), Windows Native APIs, Cross-App Cookie Authentication (no auth config found), Configuration Migration, Logging Framework, Dependency Injection (default MVC, no 3rd-party container), Nullable Reference Types, Entity Framework (already on EF Core, not EF6), Test Coverage (not recommended by assessment; user explicitly set generateNewUnitTests=false).

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (ContosoUniversity.csproj) — no dependency graph to manage; low assessment difficulty.

### Execution Constraints
- Single atomic upgrade per task — task 01 (SDK-style conversion) must complete and build on net48 before task 02 (TFM + ASP.NET Core migration) starts.
- SDK-style conversion and TFM/API-surface upgrade are never merged into one task — different failure modes.
- Task 02 is one bounded build-and-fix pass (update → restore → build → fix all compile errors), not an open-ended retry loop; run any existing tests after it builds.
- In-place web migration (no System.Web Adapters shim) — replace System.Web APIs directly with ASP.NET Core equivalents.
- Fix all build warnings in modified projects before completing each task — do not suppress warnings.
- Commit strategy: Single Commit at End (All-at-Once default) — unless execution reveals it's safer to checkpoint after task 01 (structural-only change) separately from task 02 (behavioral change); prefer committing after each task for easier rollback given the environment-specific MSBuild workaround in place (see Decisions).

## Task 02 Execution Notes (manual, due to MCP host limitation above)

- Converted `TargetFramework` to `net10.0`; `ImplicitUsings=enable`.
- Package changes: `Microsoft.Data.SqlClient` 2.1.4→7.1.1 (fixes NU1903 vuln), `Microsoft.EntityFrameworkCore*` 3.1.32→10.0.15, removed all MVC5/System.Web-era packages (`Microsoft.AspNet.Mvc/Razor/WebPages/Web.Optimization`, `Microsoft.Web.Infrastructure`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `WebGrease`, `NETStandard.Library`, `Antlr` (confirmed unused in code), jQuery/bootstrap/modernizr NuGet metadata packages — the actual static JS/CSS files were kept on disk, just no longer NuGet-managed). `Newtonsoft.Json` and `Microsoft.Windows.Compatibility` were added then **removed again** once it was discovered `System.Messaging` (MSMQ) has no .NET 10/11 compatibility shim at all (dropped from the Windows Compatibility Pack; no standalone NuGet package exists either) — see NotificationService decision below.
- **Decision — MSMQ replacement**: `Services/NotificationService.cs` used `System.Messaging` (MSMQ), which cannot be referenced on .NET 10 on any OS (Microsoft removed the compat shim; confirmed via nuspec inspection and a failed build). Replaced the MSMQ-backed queue with an in-process, cross-platform substitute built on `System.Threading.Channels.Channel<T>`, preserving the exact public API (`SendNotification`/`ReceiveNotification`/`MarkAsRead`) so controllers, the `/Notifications/GetNotifications` polling endpoint, and `notifications.js` required zero changes. Trade-off: notifications are now in-memory/per-process only (lost on restart) vs. MSMQ's durable queue — documented here since it's a behavior change forced by the platform, not a discretionary one.
- `Global.asax`/`Global.asax.cs`/`App_Start/*` deleted; replaced by `Program.cs` (minimal hosting model: `AddControllersWithViews`, `AddDbContext<SchoolContext>`, `AddScoped<NotificationService>`, endpoint routing `{controller=Home}/{action=Index}/{id?}`, `UseExceptionHandler("/Home/Error")` in non-Development as the `HandleErrorAttribute` equivalent, DB seeding via `DbInitializer` in a startup scope).
- `BaseController` modernized to constructor DI for `SchoolContext`/`NotificationService` (was manual `new`/static factory); all 6 controllers updated with explicit constructors.
- Controllers: `System.Web.Mvc`→`Microsoft.AspNetCore.Mvc`, `ActionResult`→`IActionResult`, `HttpNotFound()`→`NotFound()`, `HttpStatusCodeResult(BadRequest)`→`BadRequest()`, `[Bind(Include=...)]`→`[Bind(...)]`, `JsonRequestBehavior.AllowGet` removed. `CoursesController` file upload: `HttpPostedFileBase`→`IFormFile` + injected `IWebHostEnvironment.WebRootPath` (replacing `Server.MapPath`). `InstructorsController.Edit` POST reworked from sync string-based `TryUpdateModel` to `async Task<IActionResult>` + expression-based `TryUpdateModelAsync<Instructor>(..., i => i.LastName, ...)`. `HomeController.Unauthorized()` renamed to `AccessDenied()` (was shadowing `ControllerBase.Unauthorized()`, CS0114 warning; the action had no backing view either before or after, so behavior is unchanged — purely a naming fix).
- Static assets moved from `Content/`/`Scripts/`/`Uploads/` to `wwwroot/Content`, `wwwroot/Scripts`, `wwwroot/Uploads` (ASP.NET Core static-file convention) — all `~/Content/...`/`~/Scripts/...` tilde paths in views/code needed no changes since they resolve the same way under `wwwroot`. Removed stale `Scripts/bootstrap.js`/`bootstrap.min.js` (actually Bootstrap 3.4.1, incompatible with the Bootstrap-5 markup already in `_Layout.cshtml`) and `respond.js`/`respond.min.js` (IE8 polyfill, no longer relevant); `_Layout.cshtml` now loads Bootstrap 5.3.3 CSS/JS from CDN (matches the version declared in the old `packages.config`, for which no local `.css` file existed in the repo) alongside the local jQuery/jquery-validate/modernizr files.
- `@Styles.Render`/`@Scripts.Render` (System.Web.Optimization) replaced with direct `<link>`/`<script>` tags; 8 `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }` call sites (Departments/Courses/Students/Instructors Create+Edit) replaced with direct `jquery.validate.js`/`jquery.validate.unobtrusive.js` tags.
- `Views/Web.config` deleted, replaced by `Views/_ViewImports.cshtml` (`@using` for Models/SchoolViewModels + `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`). `Views/Shared/Error.cshtml` rewritten to use the existing `Models/ErrorViewModel` instead of `System.Web.Mvc.HandleErrorInfo`/`HttpContext.Current.IsDebuggingEnabled`.
- `Web.config` deleted (connection string/appSettings migrated to `appsettings.json`: `ConnectionStrings:DefaultConnection`, `Notifications:QueuePath` — the latter is now vestigial since the Channel-based replacement doesn't use a path, kept only for documentation continuity). `ClientValidationEnabled`/`UnobtrusiveJavaScriptEnabled`/`webpages:*` appSettings dropped (no Core equivalent needed; unobtrusive validation is built in).
- Deleted unused `Data/SchoolContextFactory.cs` (superseded by DI) and empty `Services/LoggingService.cs` (0 bytes, unreferenced).
- **Verification performed**: `dotnet build -warnaserror` → 0 errors/0 warnings. `dotnet list package --vulnerable` → none. Non-destructive smoke test: temporarily swapped the EF Core provider to `UseInMemoryDatabase` (reverted after, never committed) to work around LocalDB being unavailable on Linux, ran the app, and verified via curl: all 6 controllers' Index/Details/Create/Edit GET routes (200s), POST `Departments/Create` with antiforgery token (302 + persisted + notification emitted), POST `Instructors/Edit` using the new async `TryUpdateModelAsync` (verified nested `OfficeAssignment.Location` binding persisted correctly), POST `Courses/Create` file upload via `IFormFile` (verified file physically saved under `wwwroot/Uploads/TeachingMaterials` and rendered in the Index view), static files served from `wwwroot` (`Content/Site.css`, `Scripts/jquery-3.4.1.js`), and `/Notifications/GetNotifications` returning the Channel-based notification JSON. The genuine LocalDB `PlatformNotSupportedException` against the real `Microsoft.Data.SqlClient`/SQL Server connection string is an environment constraint (LocalDB is Windows-only) that pre-dates this migration and is unrelated to it — the committed code still targets SQL Server via `Microsoft.Data.SqlClient`/`UseSqlServer` as originally designed.
