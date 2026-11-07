# Assessment — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

> Note: The MCP `generate_dotnet_upgrade_assessment` tool could not evaluate this project —
> it requires `Microsoft.WebApplication.targets` (part of Visual Studio's Web Application
> build tools), which is only available on Windows with Visual Studio / Build Tools installed.
> This execution environment is Linux. This assessment was produced via direct manual
> inspection of the project (file reads, grep) instead.

## Project

- Solution: `ContosoUniversity/ContosoUniversity.sln`
- Project: `ContosoUniversity/ContosoUniversity.csproj` (single project, legacy/non-SDK-style, `OutputType=Library`, web application project type GUIDs)
- Current TFM: `net48` (TargetFrameworkVersion v4.8)
- Target TFM: `net10.0`
- No test project exists in the solution.

## Technology Stack (current)

- ASP.NET MVC 5 (`System.Web.Mvc` 5.2.9) — Controllers + Razor views, no Web API controllers
- Entity Framework **Core 3.1.32** already in use for data access (not EF6/EDMX) — `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.Data.SqlClient` 2.1.4 already in use (not legacy `System.Data.SqlClient`)
- `System.Messaging` (MSMQ) used by `Services/NotificationService.cs` for an admin notification feed
- `System.Web.Optimization` (`BundleConfig.cs`) for bundling jquery/bootstrap/modernizr
- `Newtonsoft.Json` used only inside `NotificationService` to serialize queue messages
- Razor views use classic `HtmlHelper` syntax (`Html.ActionLink`, `Html.BeginForm`, `Html.EditorFor`, etc.) — no child actions, no custom view engine, no `@helper` blocks
- `Web.config`: one connection string (`DefaultConnection`), a few `appSettings` keys (`NotificationQueuePath`, client-validation flags), many binding redirects (irrelevant to SDK-style/PackageReference)
- File upload in `CoursesController` (`HttpPostedFileBase`, `Server.MapPath`) for course teaching-material images, saved to `~/Uploads/TeachingMaterials/`

## Inventory

**Controllers** (all MVC, `Controller` base, no `[ChildActionOnly]`, no Web API):
- `BaseController` (abstract) — owns `SchoolContext db` + `NotificationService`, sends entity-change notifications, disposes both
- `HomeController` — Index/About/Contact/Error/Unauthorized, LINQ grouping query in `About`
- `StudentsController` — CRUD + paging/sorting/search (`PaginatedList<T>`)
- `CoursesController` — CRUD + file upload for teaching material image (`HttpPostedFileBase`, `Server.MapPath`)
- `DepartmentsController` — CRUD + optimistic concurrency handling (`RowVersion`, `DbUpdateConcurrencyException`)
- `InstructorsController` — CRUD + course-assignment checkbox list + `TryUpdateModel`
- `NotificationsController` — `GetNotifications`/`MarkAsRead` JSON endpoints (consumed only by in-repo `notifications.js` via same-origin `fetch`) + `Index` view

**Data**: `Data/SchoolContext.cs` (EF Core DbContext, already modern Fluent API, TPH inheritance for Person/Student/Instructor), `Data/SchoolContextFactory.cs` (static factory reading `ConfigurationManager`), `Data/DbInitializer.cs` (seed data, no external deps)

**Models**: `Course`, `CourseAssignment`, `Department`, `Enrollment`, `ErrorViewModel`, `Instructor`, `Notification`, `OfficeAssignment`, `Person`, `Student`, `SchoolViewModels/*` — all plain POCOs with DataAnnotations, no `System.Web` dependency. No changes required beyond namespace/project move.

**Services**: `NotificationService` (MSMQ-backed queue wrapper), `LoggingService` (empty file, unused)

**App_Start**: `RouteConfig` (one default route, one `IgnoreRoute`), `FilterConfig` (global `HandleErrorAttribute` only — no global Authorize filter, comment confirms no-auth-by-design), `BundleConfig` (script/style bundles)

**Views**: Razor views under `Views/{Home,Students,Courses,Instructors,Departments,Notifications}` + `Views/Shared/_Layout.cshtml`, `Views/_ViewStart.cshtml`, `Views/Shared/Error.cshtml`. All views use classic `HtmlHelper`s (compatible in ASP.NET Core, no TagHelper conversion strictly required) and `@Scripts.Render`/`@Styles.Render` (no Core equivalent — must be replaced with direct `<script>`/`<link>` tags).

**Root-level**: `PaginatedList.cs` (no `System.Web` dependency, usable as-is), `Global.asax`/`Global.asax.cs` (Application_Start → AreaRegistration/FilterConfig/RouteConfig/BundleConfig registration + EF Core `DbInitializer.Initialize` call), `Web.config` (connection string + appSettings + binding redirects), `packages.config`.

## Risks / Blocking Issues

1. **MSMQ (`System.Messaging`) is Windows-only** and has no functional equivalent on Linux/cross-platform .NET. The generic `migrating-to-msmq-messaging` skill would swap to the `MSMQ.Messaging` NuGet package (same API, different namespace), but that package still requires the Windows MSMQ feature/service at runtime — it would compile but throw/fail at runtime in this (Linux) environment and on any non-Windows deployment target, undermining the cross-platform value of .NET 10.
   - **Decision**: Replace the MSMQ-backed queue with an in-process, cross-platform queue (`System.Collections.Concurrent.ConcurrentQueue<Notification>`) behind the exact same `NotificationService` public surface (`SendNotification`, `ReceiveNotification`, `MarkAsRead`, `Dispose`). No controller changes needed. This is a single-process web app with no persistence requirement for the notification feed (purely a live admin UI feed), so an in-memory queue preserves observable behavior for the only consumer (the app's own `notifications.js` polling `GetNotifications`). Recorded as a decision in `scenario-instructions.md`.
2. **Wire-contract gate (`migrating-webapi-odata`) applicability check**: `NotificationsController.GetNotifications`/`MarkAsRead` return JSON. Evidence gathered: no OData package anywhere in the solution, no custom formatters/serializers, no API documentation or contract tests, and the only consumer is the same-origin `wwwroot/Scripts/notifications.js` (`fetch('/Notifications/GetNotifications')`) shipped and migrated together with the server. No externally-consumed wire contract exists.
   - **Decision**: **NOT APPLICABLE** — recorded with the evidence above. Proceeding with standard JSON controller migration. To avoid any behavior change for the in-repo JS consumer, the new host will configure `JsonSerializerOptions.PropertyNamingPolicy = null` so JSON field casing (`Operation`, `EntityType`, `CreatedAt`, etc.) is preserved exactly as the Framework host produced it (ASP.NET Core defaults to camelCase otherwise).
3. **`@Scripts.Render`/`@Styles.Render`** (System.Web.Optimization) have no ASP.NET Core equivalent — `_Layout.cshtml` will reference compiled/static `wwwroot` assets directly via `<script>`/`<link>` tags.
4. **`Server.MapPath`** in `CoursesController` → replace with `IWebHostEnvironment.WebRootPath` (injected via constructor). **`HttpPostedFileBase`** → replace with `IFormFile` (`ContentLength` → `Length`, `SaveAs(path)` → `CopyTo(stream)`/`CopyToAsync(stream)`).
5. **`Web.config` appSettings/connectionStrings** → `appsettings.json` + `IConfiguration`, binding redirects are dropped entirely (PackageReference/SDK-style resolves versions via NuGet, no bindingRedirect concept).
6. Package versions need bumping for .NET 10 compatibility: EF Core 3.1.32 → EF Core (10.x line matching target, or latest LTS-compatible 9/10), `Microsoft.Data.SqlClient` → current stable, `Microsoft.Extensions.*` → remove explicit pins (implicit via SDK/EF Core), `Newtonsoft.Json` → latest stable (still used by `NotificationService` for queue message serialization — can be kept or swapped to `System.Text.Json`; keeping Newtonsoft minimizes behavior change for that isolated internal use).
7. Client-side libraries (`bootstrap`, `jQuery`, `jQuery.Validation`, `Modernizr`, `WebGrease`, `Antlr`) currently restored via NuGet (`packages.config`) into `Content`/`Scripts` — these move to `wwwroot/lib` (can keep existing vendored files, relocated; no CDN/LibMan requirement for this exercise since there's no client-asset test coverage).

## Security Vulnerabilities

No CVE database lookup tool was available in this environment for the legacy project (assessment tool call failed before reaching package analysis). As a general modernization hygienic measure, all packages will be bumped to current stable versions compatible with `net10.0` as part of this upgrade, which resolves any known CVEs in the old `net48`-era package versions (EF Core 3.1.x is out of support and has known advisories; Newtonsoft.Json 13.0.3 is already current/patched).

## Success Criteria Mapping

| Criterion | Plan |
|---|---|
| `passBuild` | SDK-style `net10.0` ASP.NET Core project builds with `dotnet build` with 0 errors/0 warnings |
| `generateNewUnitTests=false` | No test project will be created (none exists today) |
| `passUnitTests` | Vacuously true — no test project in solution |
| `securityComplianceCheck` | All packages upgraded to current stable versions; no `System.Web`/MSMQ/legacy packages remain; no secrets in config |
