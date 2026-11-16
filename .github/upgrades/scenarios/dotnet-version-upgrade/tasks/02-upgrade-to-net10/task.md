# Task 02 — Upgrade ContosoUniversity to net10.0 and migrate to ASP.NET Core hosting model

See plan.md for the full task description. This file records research performed before
implementation, per task-execution requirements.

## Wire Compatibility (migrating-webapi-odata gate)

**Scope**: `NotificationsController.GetNotifications` / `MarkAsRead` — the only JSON-returning
endpoints in the solution (MVC `JsonResult`, not Web API).

**Inventory**:
- No OData package referenced anywhere in the solution.
- No Atom formatters, no custom `MediaTypeFormatter`/`ODataEntityTypeSerializer`.
- Consumers: `Scripts/notifications.js` only — same-origin `fetch('/Notifications/GetNotifications', { credentials: 'same-origin' })`,
  served and consumed entirely within this one application. No external API documentation,
  no third-party client bindings, no repository hint requiring preservation for outside consumers.
- **Verdict: NOT APPLICABLE** — no externally-consumed wire contract, no OData/Atom signal, no
  pinned-shape evidence beyond this app's own JS. Evidence above is sufficient per the gate's
  applicability check (in-repo-only consumer, no OData package, no documented external contract).

**Serialization Naming Inventory** (still required/useful regardless of the gate outcome, to avoid
breaking the in-repo JS consumer):
- Source: MVC 5 `JsonResult` via `Json(...)` — backed by `JavaScriptSerializer`, which **preserves
  declared CLR member names** (PascalCase) and does not honor Newtonsoft attributes.
- `Scripts/notifications.js` reads response fields `data.success`, `data.notifications`,
  `data.notifications[].Operation`, `.CreatedAt`, `.EntityType`, `.Message`, `.CreatedBy` — i.e. it
  expects **PascalCase** member names on the `Notification` objects (lowercase `success`/
  `notifications`/`count` wrapper keys are anonymous-object literals already lowercase in the C#
  source, so those pass through unchanged either way).
- Naming policy chosen for ASP.NET Core: **Option A (System.Text.Json) with
  `PropertyNamingPolicy = null`** — this preserves CLR declared names exactly, matching the MVC 5
  source behavior and requiring zero changes to `notifications.js`.
- No custom converters, no dictionary-keyed payloads, no per-action overrides found.

## MSMQ (System.Messaging) — `migrating-to-msmq-messaging` skill

`Services/NotificationService.cs` uses `System.Messaging.MessageQueue`/`Message`/`XmlMessageFormatter`,
configured via `ConfigurationManager.AppSettings["NotificationQueuePath"]`. `System.Messaging` is
Windows-only and not available on modern .NET without a replacement package.
Replacement: `MSMQ.Messaging` NuGet package (1.0.4, confirmed net10.0-compatible via
`get_supported_package_version`) — API-compatible (same type names, `MSMQ.Messaging` namespace).
Queue path configuration moves from `ConfigurationManager.AppSettings` to `appsettings.json` +
`IOptions<MessageQueueOptions>`, injected via DI.

## Package plan (net10.0 targets, from assessment + `get_supported_package_version`)

| Package | Action | Version |
|---|---|---|
| Microsoft.EntityFrameworkCore(.*) | Upgrade | 10.0.15 |
| Microsoft.Data.SqlClient | Upgrade (fixes NU1903 vulnerability) | 7.1.2 |
| Microsoft.Data.SqlClient.SNI.runtime | Remove — obsolete with modern Microsoft.Data.SqlClient (native assets resolved automatically) | — |
| Microsoft.Extensions.* (Caching, Configuration, DependencyInjection, Logging, Options, Primitives) | Upgrade | 10.0.15 |
| Microsoft.Bcl.AsyncInterfaces / Microsoft.Bcl.HashCode | Upgrade (where still needed) / drop if no longer referenced once on net10.0 (BCL types are built-in) | — |
| Newtonsoft.Json | Upgrade (still used by `NotificationService` for queue message payloads) | 13.0.4 |
| System.Runtime.CompilerServices.Unsafe, System.Collections.Immutable, System.Diagnostics.DiagnosticSource | Drop — part of net10.0 framework/BCL, no longer needed as explicit packages | — |
| Microsoft.AspNet.Mvc / .Razor / .WebPages / .Web.Optimization / .Web.Infrastructure, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, NETStandard.Library, System.Buffers, System.ComponentModel.Annotations, System.Memory, System.Numerics.Vectors, System.Threading.Tasks.Extensions | Remove — System.Web-era packages/BCL polyfills, superseded by ASP.NET Core / net10.0 framework reference | — |
| Antlr, WebGrease, Microsoft.Identity.Client | Remove — confirmed unused in code (grep found zero references); were transitive tooling for the removed Optimization/bundling pipeline or unused leftovers | — |
| bootstrap, jQuery, jQuery.Validation, Microsoft.jQuery.Unobtrusive.Validation, Modernizr | Remove NuGet package references — these only deliver static JS/CSS content-files; the physical files already exist under `Scripts/`/`Content/` and are served as static assets under ASP.NET Core's `wwwroot`-free static file middleware (no package functionality used at runtime) | — |
| Microsoft.AspNet.Web.Optimization (re-added in task 01 to keep net48 compiling) | Remove — bundling replaced with static `<script>`/`<link>` tags per Unsupported Packages = Resolve Inline | — |
| MSMQ.Messaging (new) | Add | 1.0.4 |

## Hosting-model migration checklist (migrating-aspnet-framework-to-core, in-place mode)

- `Program.cs`: minimal hosting, `AddControllersWithViews()`, JSON options (naming policy above),
  `AddDbContext<SchoolContext>` (or keep factory-based creation — DI-friendly `AddDbContextFactory`
  since `SchoolContextFactory` already exists), register `NotificationService` + `MessageQueueOptions`,
  `MapControllerRoute` (ports `RouteConfig`), `UseStaticFiles()`, `UseExceptionHandler("/Home/Error")`
  (ports `FilterConfig`'s `HandleErrorAttribute`).
- Remove `Global.asax`, `Global.asax.cs`, `App_Start/RouteConfig.cs`, `App_Start/BundleConfig.cs`,
  `App_Start/FilterConfig.cs` once ported.
- Controllers: `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`; constructor-inject `SchoolContext`
  and `NotificationService` instead of `BaseController`'s field-initializer `new NotificationService()`
  / `SchoolContextFactory.Create()` (DI-friendly, scoped lifetime).
- `CoursesController`: `HttpPostedFileBase` → `IFormFile`; `Server.MapPath` → `IWebHostEnvironment.WebRootPath`/`ContentRootPath`.
- Views: add `Views/_ViewImports.cshtml` with `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`;
  replace `@Scripts.Render`/`@Styles.Render` in `_Layout.cshtml` (and any other views using them)
  with static `<script>`/`<link>` tags; audit `@Html.*` helpers for TagHelper conversion.
- `Web.config` → `appsettings.json` (`DefaultConnection` connection string, `NotificationQueuePath`
  → `MessageQueue:QueuePath`); remove `Web.config`, `Web.Debug.config`, `Web.Release.config`,
  `Global.asax` and the 22 binding-redirect entries (moot — .NET Core doesn't use them).
- No session/TempData/Forms-Auth/third-party DI/third-party logging/OWIN in this app — none of
  those satellite skills apply.
