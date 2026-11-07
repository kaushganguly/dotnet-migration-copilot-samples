# Progress — 02-host-and-configuration

## What changed
- Added `Program.cs` (minimal hosting model): registers `AddControllersWithViews()` with
  `PropertyNamingPolicy = null` (keep PascalCase JSON to match existing `notifications.js`),
  `AddDbContext<SchoolContext>` wired to `ConnectionStrings:DefaultConnection`, `NotificationService`
  as a singleton (in-process queue must be shared across requests — see task 03), static files +
  routing middleware, single default route (ported 1:1 from `RouteConfig.cs`), and DB seeding via
  `DbInitializer.Initialize` in a startup scope (ported 1:1 from `Global.asax.cs`'s
  `InitializeDatabase`).
- Added `appsettings.json` — `ConnectionStrings:DefaultConnection` (same LocalDB value as
  `Web.config`), `Notifications:QueuePath` (same value as the old `NotificationQueuePath`
  appSetting, consumed by task 03's rewritten `NotificationService`).
- Added `appsettings.Development.json` (standard empty override placeholder).
- `webpages:*`/`ClientValidationEnabled`/`UnobtrusiveJavaScriptEnabled` appSettings were
  intentionally NOT carried over — MVC5 bundling/unobtrusive-validation feature flags with no
  Core equivalent/meaning.
- Left `Global.asax(.cs)`, `App_Start/*.cs`, `Web.config` + variants in place (dead code once
  `Program.cs` exists under `Microsoft.NET.Sdk.Web`) — scheduled for deletion in task 06 cleanup.

## Validation
- `dotnet build` run to confirm scope of remaining work: fails only on not-yet-migrated files
  (`Controllers/*.cs` still using `System.Web.Mvc` attributes/types, `Models`/`Views` references) —
  expected at this stage of an in-place, task-ordered rewrite. No errors originate from
  `Program.cs`/`appsettings.json` themselves.

## Files Modified
- Program.cs (added)
- appsettings.json (added)
- appsettings.Development.json (added)
