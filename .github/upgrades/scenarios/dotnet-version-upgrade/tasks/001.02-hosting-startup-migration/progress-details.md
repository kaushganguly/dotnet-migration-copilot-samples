# Progress — 001.02-hosting-startup-migration

## Changes
- Added `Program.cs` (ASP.NET Core minimal hosting model): `AddDbContext<SchoolContext>` wired
  to `ConnectionStrings:DefaultConnection`, `NotificationService` registered as a DI singleton,
  `AddControllersWithViews()` with `JsonSerializerOptions.PropertyNamingPolicy = null` (keeps
  PascalCase JSON so `wwwroot/Scripts/notifications.js` needs no changes), static files,
  default MVC route `{controller=Home}/{action=Index}/{id?}`, and a startup scope that calls
  `DbInitializer.Initialize` (replaces `Global.asax.cs` `Application_Start`).
- Added `appsettings.json` (`ConnectionStrings:DefaultConnection` — same LocalDB connection
  string that was in `Web.config`) and `appsettings.Development.json`.
- Decision: did **not** carry forward `NotificationQueuePath` from the old `Web.config`
  `<appSettings>` — the new `NotificationService` (task 001.03) uses an in-process
  `ConcurrentQueue`, which has no path/location concept. Documented here since this is a
  behavior-relevant drop, not an oversight.
- Removed `Global.asax`, `Global.asax.cs`, `App_Start/RouteConfig.cs`, `App_Start/FilterConfig.cs`,
  `App_Start/BundleConfig.cs` (empty `App_Start/` folder removed), `Web.config`, `Views/Web.config`
  via `git rm`.
- Moved `Content/`, `Scripts/`, `Uploads/` to `wwwroot/Content`, `wwwroot/Scripts`,
  `wwwroot/Uploads` via `git mv` (preserves history, keeps the same relative sub-paths so
  `~/Content/...`, `~/Scripts/...`, `~/Uploads/...` references elsewhere in the app keep
  resolving once ASP.NET Core static file serving is enabled).

## Validation
- Directory structure confirmed via `find`/`ls` after the moves — no legacy hosting files
  remain outside `wwwroot`. Full build deferred to task 001.05 (controllers/views still use
  `System.Web.*` until tasks 001.03/001.04 land).
