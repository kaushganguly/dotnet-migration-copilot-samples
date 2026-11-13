# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Summary

Single-project, in-place conversion of `ContosoUniversity.csproj` from a legacy (non-SDK-style)
ASP.NET MVC 5 / System.Web application targeting .NET Framework 4.8, to an SDK-style ASP.NET Core
MVC application targeting `net10.0`. No side-by-side host — the project is rewritten directly.
There are no test projects in the solution.

## Scope

- **Project**: `ContosoUniversity.csproj` (only project in `ContosoUniversity.sln`)
- **Target**: `net10.0`
- **Approach**: In-place rewrite

## Key Findings (from assessment / manual review)

- Legacy csproj (`ToolsVersion="15.0"`, `packages.config`), Web Application project type.
- 7 controllers (`Base`, `Home`, `Students`, `Courses`, `Instructors`, `Departments`,
  `Notifications`), all MVC (no Web API / `ApiController`).
- No Forms/Windows auth, no Membership, no OWIN, no custom DI container, no Session/HttpModules/
  HttpHandlers. Default route only (`RouteConfig`). `TempData` used once (works out of the box
  with ASP.NET Core's cookie-based TempData provider).
- `BaseController` currently does `new NotificationService()` and
  `SchoolContextFactory.Create()` (which reads `ConfigurationManager.ConnectionStrings`) in its
  constructor — migrate to constructor DI (`SchoolContext`, `NotificationService` registered in
  `Program.cs`), with per-derived-controller constructors forwarding to `base(...)`.
  `AddDbContext<SchoolContext>` with `Scoped` lifetime; `NotificationService` as `Scoped` (wraps
  an MSMQ connection) or `Singleton` per thread-safety — register as `Scoped` to match prior
  per-request semantics.
- `CoursesController` does image upload via `HttpPostedFileBase` + `Server.MapPath` — migrate to
  `IFormFile` + injected `IWebHostEnvironment.WebRootPath`.
- `Services/NotificationService.cs` uses `System.Messaging` (MSMQ) — not available on
  .NET Core/5+; migrate to the `MSMQ.Messaging` NuGet package (same type/method names, new
  namespace) per `migrating-to-msmq-messaging`.
- Views use standard `HtmlHelper` calls (`Html.BeginForm`, `Html.EditorFor`,
  `Html.ValidationSummary`, `Html.ActionLink`, `Html.DropDownList`, etc.) — all available in
  ASP.NET Core's `IHtmlHelper` without substitution. `@Scripts.Render` / `@Styles.Render`
  (`System.Web.Optimization` bundling) have no built-in ASP.NET Core equivalent — replace with
  direct `<script>`/`<link>` tags against `wwwroot/`.
- `[Bind(Include = "...")]` property syntax → ASP.NET Core positional `[Bind("...")]`.
- `HttpNotFound()` → `NotFound()`; `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` →
  `BadRequest()`; `Json(obj, JsonRequestBehavior.AllowGet)` → `Json(obj)`.
- `Instructor.Edit (POST)` uses `TryUpdateModel(model, prefix, string[] includeProperties)` —
  ASP.NET Core's `TryUpdateModelAsync` only supports expression-based include lists; action
  becomes `async Task<ActionResult>`.
- Packages: EF Core 3.1.32 → latest EF Core compatible with net10.0 (10.x);
  Microsoft.Data.SqlClient → latest; Newtonsoft.Json kept (used by `NotificationService` for
  queue message serialization) → latest 13.x; remove packages with no Core equivalent
  (`Microsoft.AspNet.Mvc`, `Microsoft.AspNet.WebPages`, `Microsoft.AspNet.Razor`,
  `Microsoft.AspNet.Web.Optimization`, `WebGrease`, `Antlr`, `Microsoft.Web.Infrastructure`,
  `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.jQuery.Unobtrusive.Validation`
  (replaced by client npm/static assets), `NETStandard.Library`, BCL polyfill packages now part
  of the runtime).
- No known CVEs flagged in the (limited) assessment output; latest stable package versions are
  used regardless as part of the upgrade.

## Tasks

See `tasks.md` for the authoritative, tool-managed checklist. Single execution task
(in-place mode executes all migration steps sequentially within one task):

1. **001-upgrade-dotnet-to-net10** — Convert project to SDK-style targeting `net10.0`
   (`Microsoft.NET.Sdk.Web`), migrate packages, create ASP.NET Core host (`Program.cs`),
   migrate configuration (`Web.config` → `appsettings.json`), wire DI, migrate controllers,
   migrate views/static assets, migrate `System.Messaging` → `MSMQ.Messaging`, clean up legacy
   files, build and validate.

## Validation

- `dotnet build` (or `dotnet build ContosoUniversity.sln`) — 0 errors, 0 warnings in the migrated
  project.
- No test projects exist in the solution — `passUnitTests` is trivially satisfied once the build
  succeeds (nothing to run).
- No remaining `System.Web`, `System.Messaging`, `packages.config`, or legacy MSBuild import
  references.
