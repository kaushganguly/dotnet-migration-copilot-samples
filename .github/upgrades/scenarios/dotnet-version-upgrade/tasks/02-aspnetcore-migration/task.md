# 02-aspnetcore-migration: Migrate ASP.NET MVC 5 to ASP.NET Core MVC on .NET 10.0

## Objective
Complete the upgrade of ContosoUniversity from the SDK-style net48 project produced by Task 01 to
a fully functioning ASP.NET Core MVC application targeting .NET 10.0, migrating off `System.Web`,
`Global.asax`, bundling/optimization, and `Web.config` entirely.

## Scope (from plan.md)
- Retarget TFM to `net10.0`, SDK `Microsoft.NET.Sdk.Web`.
- Replace `Global.asax`/`Global.asax.cs` + `App_Start/*` with `Program.cs` minimal hosting model.
- Replace `System.Web.Mvc` controller base/action result types with `Microsoft.AspNetCore.Mvc`
  equivalents across all 6 controllers + `BaseController`.
- Move DI-unfriendly manual object construction (`SchoolContext`, `NotificationService`) in
  `BaseController` to constructor injection.
- Remove `System.Web.Optimization` bundling (`BundleConfig`); replace with direct `<link>`/
  `<script>` tags in `_Layout.cshtml` and per-view `@section Scripts` blocks.
- Move `Content/`, `Scripts/`, `Uploads/` under `wwwroot/` for ASP.NET Core static file serving.
- Migrate `Web.config` `appSettings`/`connectionStrings` to `appsettings.json`.
- Replace `System.Messaging` (MSMQ) in `NotificationService` — no .NET 10 equivalent exists.
- Convert `HttpPostedFileBase` file-upload logic in `CoursesController` to `IFormFile`.
- Update all NuGet packages to .NET 10-compatible versions; drop unused legacy packages.
- Fix the `Microsoft.Data.SqlClient` security vulnerability flagged in assessment.md (upgrade to a
  patched version).

## Repo facts discovered during research
- Routing: single default route only (`{controller}/{action}/{id}`), no attribute routing, no
  areas (`App_Start/RouteConfig.cs`). Global filter: `HandleErrorAttribute` only (`FilterConfig.cs`).
- No `HttpContext.Current`, `Session`, `[Authorize]`, or third-party DI container usage anywhere
  in the codebase — simplifies the DI/auth migration considerably.
- `Services/NotificationService.cs` used `System.Messaging` (MSMQ), a Windows-only API with no
  .NET 10 equivalent. The queue was a simple, non-durable, in-process hand-off (confirmed: no
  other consumers, no persistence requirement in the original code) — replaced with a
  `ConcurrentQueue<Notification>` DI singleton, preserving the original public API surface
  (`Enqueue`/dequeue-style methods) so `BaseController` and `NotificationsController` needed no
  behavioral changes beyond removing the MSMQ-specific plumbing.
- `NotificationsController` exposes two `JsonResult` actions (`GetNotifications`, `MarkAsRead`).
  Checked against the Wire-Contract Gate (`migrating-webapi-odata` / `migrating-mvc-content-
  negotiation` skills): the only consumer is `Scripts/notifications.js`, migrated in this same
  in-place task — NOT an externally-consumed contract, so the gate does not block migrating these
  actions to `Microsoft.AspNetCore.Mvc`. To avoid any silent behavior change for the JS client
  (MVC5 `JsonResult` used `JavaScriptSerializer`, which does NOT camelCase member names), JSON
  serialization is configured with `PropertyNamingPolicy = null` / `DictionaryKeyPolicy = null` in
  `Program.cs` so PascalCase property names are preserved exactly.
- `CoursesController` file-upload logic used `HttpPostedFileBase` + `.SaveAs()` +
  `Server.MapPath()`, writing to `~/Uploads/TeachingMaterials/{file}`. Converted to `IFormFile` +
  `IWebHostEnvironment.WebRootPath` + manual `FileStream.CopyTo()`. Since `Uploads/` was moved
  under `wwwroot/Uploads/`, the existing `~/Uploads/...` URL paths stored in the DB continue to
  resolve correctly via ASP.NET Core static file middleware without any path-format changes.
- Original `Web.config` also set `<httpRuntime maxRequestLength="10240" executionTimeout="3600" />`
  and `<requestLimits maxAllowedContentLength="10485760" />` (10 MB / 1-hour timeout) for large
  teaching-material uploads. Preserved via explicit Kestrel (`MaxRequestBodySize`,
  `KeepAliveTimeout`) and `FormOptions.MultipartBodyLengthLimit` configuration in `Program.cs`.
- `Data/SchoolContextFactory.cs` (an EF Core design-time factory, used only for `dotnet ef`
  tooling under the old non-DI-friendly constructor pattern) became unreferenced once
  `SchoolContext`'s existing `DbContextOptions<SchoolContext>` constructor was wired through
  `AddDbContext` in `Program.cs` — confirmed zero remaining references, removed.
- `Antlr3.Runtime`, `WebGrease`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`,
  `Microsoft.Identity.Client`, `Microsoft.Bcl.HashCode`, `Microsoft.Web.Infrastructure` were all
  transitive-only dependencies of the old MVC5/Optimization/Razor stack with zero direct code
  usage anywhere in the repo — confirmed via repo-wide search, dropped entirely rather than
  replaced (assessment.md's note about an "Antlr4 replacement" does not apply since there was no
  direct usage to replace).
- No test project exists in `ContosoUniversity.sln` (single-project solution) — confirmed via
  `.sln` inspection. `generateNewUnitTests=false` per user's success criteria, so this is expected;
  `passUnitTests=true` holds vacuously (no test suite exists to fail).
- `Views/Shared/Error.cshtml` used `@model System.Web.Mvc.HandleErrorInfo` and
  `HttpContext.Current.IsDebuggingEnabled` — both incompatible with ASP.NET Core. Rewritten to use
  the already-present `Models/ErrorViewModel.cs` (`RequestId`/`ShowRequestId`), following the
  standard ASP.NET Core project template's error-page pattern. `HomeController.Error()` updated to
  construct and pass the view model.

## Target package versions (resolved via get_supported_package_version for net10.0)
- `Microsoft.EntityFrameworkCore` / `.Relational` / `.SqlServer` / `.Tools` → 10.0.15
- `Microsoft.Data.SqlClient` → 7.1.2 (resolves the NU1903 vulnerability flagged in assessment.md)
- `Newtonsoft.Json` → 13.0.4

## Done when
- Project targets `net10.0`, SDK-style, `Microsoft.NET.Sdk.Web`.
- No `System.Web`/`Global.asax`/`Web.config`/`packages.config` references remain anywhere in the
  repo (controllers, views, project file).
- `dotnet build` (both `ContosoUniversity.csproj` directly and the full `ContosoUniversity.sln`)
  succeeds with 0 errors / 0 warnings.
- `dotnet list package --vulnerable --include-transitive` reports no vulnerable packages.
- Documentation (`README.md`, `NOTIFICATION_SYSTEM_README.md`, `SETUP_TESTING_GUIDE.md`,
  `TEACHING_MATERIAL_UPLOAD.md`) updated to reflect the new stack and the MSMQ → in-memory queue
  replacement, so they no longer describe obsolete Framework/MSMQ-era setup as current.
