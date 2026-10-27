# 001.02-hosting-startup-migration: Program.cs, wwwroot layout, remove Global.asax/App_Start

## Objective
Replace the ASP.NET (System.Web) hosting model with ASP.NET Core's minimal hosting model.

## Steps
1. Add `Program.cs` (top-level statements): `WebApplicationBuilder`, register `SchoolContext` via `AddDbContext` using `builder.Configuration.GetConnectionString("DefaultConnection")`, register `NotificationService` as singleton, `AddControllersWithViews()` with `JsonOptions.PropertyNamingPolicy = null` (preserve PascalCase JSON for notifications.js), default MVC route (`{controller=Home}/{action=Index}/{id?}`), `UseStaticFiles()`, run `DbInitializer.Initialize` against a scoped `SchoolContext` at startup.
2. Delete `Global.asax`, `Global.asax.cs`, `App_Start/RouteConfig.cs`, `App_Start/FilterConfig.cs`, `App_Start/BundleConfig.cs` (bundling replaced by direct `<script>`/`<link>` tags in `_Layout.cshtml`).
3. Create `appsettings.json` with `ConnectionStrings:DefaultConnection` (same LocalDB connection string previously in Web.config) and `NotificationQueuePath` setting (retained as a no-op/compat setting, or dropped since the in-memory queue no longer needs a path — document the decision).
4. Move `Content/`, `Scripts/`, `Uploads/` directories under `wwwroot/` (same relative sub-paths) so existing `~/Content/...`, `~/Scripts/...`, `~/Uploads/...` references in views/controllers keep resolving via ASP.NET Core static file serving.
5. Delete `Web.config`, `Views/Web.config` (no longer applicable under ASP.NET Core).
