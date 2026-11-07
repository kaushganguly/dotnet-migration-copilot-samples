# 02-host-and-configuration

## Objective
Create the ASP.NET Core minimal-hosting entry point (`Program.cs`) and migrate `Web.config`
to `appsettings.json`, replacing Global.asax/App_Start registration with Program.cs wiring.

## Research Findings (confirmed against repo)
- `Global.asax.cs`: `Application_Start` calls `AreaRegistration.RegisterAllAreas()` (no areas
  exist in this app — no-op, skip), `FilterConfig.RegisterGlobalFilters` (only registers
  `HandleErrorAttribute` — maps to `app.UseExceptionHandler()`/developer exception page),
  `RouteConfig.RegisterRoutes` (one default route + one `IgnoreRoute` for `*.axd` — not needed
  in Core), `BundleConfig.RegisterBundles` (dropped — System.Web.Optimization has no Core
  equivalent, task 05 replaces with direct `<link>/<script>` tags), then calls
  `InitializeDatabase()` which creates a `SchoolContext` via `DbContextOptionsBuilder` reading
  `ConfigurationManager.ConnectionStrings["DefaultConnection"]` and calls `DbInitializer.Initialize`.
- `Web.config`: one connection string `DefaultConnection` (LocalDB, dev-only value — will be
  preserved as the default/dev value since no other environment config exists), `appSettings`:
  `webpages:Version`/`webpages:Enabled`/`ClientValidationEnabled`/`UnobtrusiveJavaScriptEnabled`
  (all System.Web.Mvc/WebPages specific — obsolete in Core, drop) and `NotificationQueuePath`
  (kept — consumed by task 03's new in-process `NotificationService`, moved under a
  `Notifications:QueuePath` config section for IOptions binding consistency).
- `RouteConfig.cs` has exactly one route: `{controller}/{action}/{id}` defaults
  `controller=Home, action=Index, id=Optional` → `app.MapControllerRoute(name: "default",
  pattern: "{controller=Home}/{action=Index}/{id?}")` per `migrating-mvc-routing` skill mapping.
- No `Web.Debug.config`/`Web.Release.config` XML transforms contain anything beyond the
  base file needs (standard VS scaffolding, essentially empty transform stubs) — confirmed by
  inspection; `appsettings.Development.json` will just carry the same dev connection string,
  no separate values needed.

## Steps
1. Create `Program.cs`: `WebApplication.CreateBuilder(args)`;
   `builder.Services.AddControllersWithViews().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null)`;
   `builder.Services.AddDbContext<SchoolContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")))`;
   register `NotificationService` (singleton — queue must be shared across requests, task 03
   defines it); `app.UseStaticFiles()`; `app.UseRouting()`; `app.MapControllerRoute(...)`;
   seed DB via `DbInitializer.Initialize` using a scoped service provider at startup.
2. Create `appsettings.json` with `ConnectionStrings:DefaultConnection` (same LocalDB string as
   `Web.config`) and `Notifications:QueuePath`.
3. Create `appsettings.Development.json` (empty override object `{}` — placeholder for
   environment-specific future use, matches ASP.NET Core project template convention).
4. Leave `Global.asax`, `Global.asax.cs`, `App_Start/*.cs`, `Web.config`, `Web.Debug.config`,
   `Web.Release.config`, `Views/Web.config` in place for now — they are removed in task 06
   (cleanup) once nothing references them, to avoid breaking in-progress git history/diffs
   mid-migration. They are already dead code once `Program.cs` exists (nothing invokes
   `Global.asax` under `Microsoft.NET.Sdk.Web`).

## Files Added
- Program.cs
- appsettings.json
- appsettings.Development.json
