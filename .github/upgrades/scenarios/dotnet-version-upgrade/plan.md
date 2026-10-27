# .NET Upgrade Plan — ContosoUniversity

## Summary

| | |
|---|---|
| **Source** | .NET Framework 4.8, ASP.NET MVC 5 (legacy/non-SDK-style project) |
| **Target** | .NET 10.0 (LTS), ASP.NET Core MVC (SDK-style) |
| **Solution** | ContosoUniversity.sln (1 project) |

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-once | Single project, no dependency graph to order. |
| Project Approach | In-place rewrite | User explicitly requested converting the existing project to net10.0 SDK-style, not a side-by-side/new project. |
| Unsupported Packages | Replace with net10.0-compatible equivalents | Microsoft.AspNet.Mvc/Razor/WebPages/Web.Optimization/Web.Infrastructure/CodeDom.Providers/NETStandard.Library are obsolete under ASP.NET Core and are removed; replaced by ASP.NET Core MVC built-ins. |
| System.Web Adapters | Not used | Full in-place rewrite to ASP.NET Core MVC was requested, not a System.Web compatibility shim. |
| Assembly Binding Redirects | Removed | SDK-style/net10.0 projects use PackageReference version unification instead of binding redirects. |
| Configuration Migration | Web.config → appsettings.json | `<appSettings>`/`<connectionStrings>` migrated to `appsettings.json` + `IConfiguration`. |
| Entity Framework | EF Core 3.1.32 → EF Core 10.0.x | Aligns EF Core major version with net10.0 target. |

## Why Upgrade

.NET Framework 4.8 is the final release of .NET Framework and receives no further investment.
The project also mixes .NET Framework with EF Core 3.1 (an unsupported, out-of-support
combination). Moving to .NET 10 (LTS) gives long-term support, current security patches, a
modern EF Core release, and a cross-platform, higher-performance ASP.NET Core MVC hosting model.

## Scope of Work

1. **Project file modernization** — Convert `ContosoUniversity.csproj` from legacy
   (`packages.config`, non-SDK-style) format to SDK-style (`Microsoft.NET.Sdk.Web`) targeting
   `net10.0`, with `PackageReference` items.
2. **Hosting/startup migration** — Replace `Global.asax`/`Global.asax.cs` + `App_Start/*`
   (`RouteConfig`, `FilterConfig`, `BundleConfig`) with a `Program.cs` using the ASP.NET Core
   minimal hosting model: DI registration (`SchoolContext`, `NotificationService`), default MVC
   route, static files, and startup database initialization.
3. **Controller migration** — Port all controllers (`Home`, `Students`, `Courses`,
   `Departments`, `Instructors`, `Notifications`, `BaseController`) from `System.Web.Mvc` to
   `Microsoft.AspNetCore.Mvc`: `HttpNotFound()` → `NotFound()`, `HttpStatusCodeResult` →
   `StatusCode()`/`BadRequest()`, `[Bind(Include=...)]` → `[Bind("...")]`,
   `HttpPostedFileBase` → `IFormFile`, `Server.MapPath` → `IWebHostEnvironment`,
   `TryUpdateModel` → `TryUpdateModelAsync`, `JsonRequestBehavior` removed, DI-based
   construction instead of `SchoolContextFactory.Create()` / `new NotificationService()`.
4. **View migration** — Most Razor `Html.*` helpers (`BeginForm`, `EditorFor`,
   `DisplayFor`, `ValidationMessageFor`, `ActionLink`, `DropDownList`) carry over directly to
   ASP.NET Core MVC. Replace `@Scripts.Render`/`@Styles.Render` (System.Web.Optimization,
   removed) with direct `<script>`/`<link>` tags. Replace the `Error.cshtml`
   `System.Web.Mvc.HandleErrorInfo` model with the existing `ErrorViewModel`. Add
   `_ViewImports.cshtml` for the namespaces/usings Core views need. Move `Content/`, `Scripts/`,
   `Uploads/` under `wwwroot/` (same relative sub-paths, so existing `~/Content/...`,
   `~/Scripts/...` references keep working via ASP.NET Core static files).
5. **NotificationService modernization** — Replace `System.Messaging` (MSMQ, Windows-only,
   unsupported on .NET Core/.NET 10) with an in-process `ConcurrentQueue<Notification>`-backed
   singleton service, keeping the same public API (`SendNotification`, `ReceiveNotification`,
   `MarkAsRead`, `Dispose`) so `NotificationsController` and `notifications.js` keep working
   unchanged (including the PascalCase JSON shape the client JS expects — configured via
   `JsonSerializerOptions.PropertyNamingPolicy = null`).
6. **Entity Framework Core upgrade** — Upgrade `Microsoft.EntityFrameworkCore*` packages from
   3.1.32 to the net10.0-aligned 10.0.x line; `SchoolContext` already uses stable EF Core APIs
   (`OnModelCreating`, `ToTable`, `HasDiscriminator`, TPH, composite keys) that carry forward
   unchanged. Add a `SchoolContextDesignTimeFactory : IDesignTimeDbContextFactory<SchoolContext>`
   reading `appsettings.json` for `dotnet-ef` tooling; remove the old `ConfigurationManager`-based
   runtime factory in favor of DI registration in `Program.cs`.
7. **Configuration migration** — Move `<connectionStrings>`/`<appSettings>` from `Web.config`
   into `appsettings.json`, read via `IConfiguration`/`builder.Configuration`.
8. **Build & validation** — Build on net10.0, fix any remaining compiler errors/warnings, and
   run existing tests (none exist in this repo today, so this criterion is trivially satisfied;
   a smoke-level `dotnet build` + manual route sanity check stands in for test coverage).

## Out of Scope

- New feature development, UI redesign, authentication/authorization (none existed before).
- Containerization or Azure deployment.
- Reinstating MSMQ-based messaging (Windows-only, unsupported on cross-platform .NET).

## Risks

- **MSMQ (`System.Messaging`)** has no supported .NET 10 equivalent — mitigated by switching to
  an in-memory notification queue (documented behavior change: notifications no longer persist
  across process restarts).
- **File uploads** (`Course` teaching-material image) move from `Server.MapPath` +
  `HttpPostedFileBase.SaveAs` to `IWebHostEnvironment.WebRootPath` + `IFormFile.CopyToAsync`.
- **Sandbox environment**: Linux-only, no Visual Studio `msbuild.exe`. The legacy project's
  `Microsoft.WebApplication.targets` import does not exist here; a local no-op stub was used to
  unblock MSBuild *evaluation* for assessment tooling only (not part of the repo, and moot once
  the project becomes SDK-style and drops that import).
