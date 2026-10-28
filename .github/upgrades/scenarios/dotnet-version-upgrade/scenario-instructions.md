# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj (ASP.NET MVC 5, .NET Framework 4.8, non-SDK-style)
- Target framework: net10.0

## User Preferences

### Execution Style
- **Flow Mode: Automatic** — do not pause for user review/approval at any stage. Only stop for genuine blockers. Surface assessment/plan/progress without blocking.

### Technical Preferences
- Accept default/recommended target framework and package versions suggested by tooling.
- SDK-style project conversion required as part of this upgrade.
- generateNewUnitTests: false (per task success criteria) — do not author new unit tests; fix/run existing ones only.
- Success criteria: build passes, existing unit tests pass, security/compliance check (no known vulnerable packages).

## Decisions
- Project is an ASP.NET MVC 5 (System.Web-based) application. System.Web has no direct equivalent in .NET 10 — migrating target framework to net10.0 requires porting to ASP.NET Core (MVC) APIs as part of this upgrade, since .NET 10 (net10.0) does not support System.Web-based ASP.NET MVC. This will be handled as part of the assessment/plan (framework + web stack migration), without pausing for confirmation (Automatic mode), using standard ASP.NET Core MVC equivalents for controllers/views/razor/routing/bundling/config.
- `get_dotnet_upgrade_options` failed on this legacy web project (missing Microsoft.WebApplication.targets under the .NET SDK path) — expected for a non-SDK legacy web project; proceeding with assessment via `generate_dotnet_upgrade_assessment` directly.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects); System.Web Libraries: In-place (fixed)

### Compatibility
- Unsupported Packages: Resolve Inline (1 package without known replacement: Microsoft.AspNet.Web.Optimization)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects
- Nullable Reference Types: Leave Disabled

### Rationale notes
- Single project (1 total), .NET Framework detected -> All-at-Once strategy (fixed, no dependency graph to manage).
- Web project is small (7 controllers incl. base, ~2000 LOC in .cs + 27 views), low assessment difficulty, no live-traffic/downtime constraint (sample/demo repo) -> In-place rewrite chosen over Side-by-side.
- No `HttpContext.Current` usage found anywhere in the codebase, small project, in-place approach confirmed -> Direct Migration to ASP.NET Core APIs (System.Web Adapters shim not needed).
- Binding redirects in Web.config are standard VS auto-generated boilerplate (no hand-authored or major-version-jump redirects) -> Remove Binding Redirects; SDK-style projects auto-manage assembly resolution.
- Migration is already a full System.Web -> ASP.NET Core rewrite (high-risk/demanding) -> Leave Nullable Reference Types disabled for this pass; can be enabled separately later.
- No third-party DI container, logging framework, or cookie authentication detected -> those options not applicable.
- EF Core 3.1 already in use (not EF6) -> Entity Framework option not applicable; will upgrade EF Core to a version compatible with net10.0 (10.0.x) as part of package upgrades.

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (1 total), net48 → net10.0, no inter-project dependency graph.

### Execution Constraints
- Single atomic upgrade — the one project is updated in a single in-place rewrite pass
- Validate full solution build after the upgrade completes (one bounded build-and-fix pass, not an open-ended retry loop)
- In-Place web migration mode (per `migrating-aspnet-framework-to-core`): execute all migration steps sequentially within this one task (project file/SDK → host → config → DI → controllers → middleware → views/static assets → cleanup/verification) — no task boundaries between steps
- No test projects exist in the solution — skip test-baseline tasks; `passUnitTests` criterion is vacuous

## Source Control
- Working branch: upgrade/net10-contoso-university
- Commit Strategy: Single Commit at End (All-at-Once strategy default)

## Execution Summary (completed via direct code migration)

`start_task`/`complete_task` could not be used for task `001-upgrade-dotnet-to-net10` /
`01-migrate-to-aspnetcore-net10` due to a persistent MCP backend failure
("plan.md exists but could not be parsed into tasks... restore its internal LLM client"),
confirmed non-transient across multiple retries and task-id variants. Per the user's
explicit Automatic-mode, fully-autonomous instruction, the migration was executed directly:

- Converted `ContosoUniversity.csproj` to SDK-style (`Microsoft.NET.Sdk.Web`, `net10.0`,
  `PackageReference`s: EF Core 10.0.14 (+SqlServer/Relational), Newtonsoft.Json 13.0.4,
  pinned `System.Security.Cryptography.Pkcs` 10.0.14 to resolve a transitive NU1902/NU1903
  vulnerability from Microsoft.Data.SqlClient). Removed `packages.config`.
- Replaced `Global.asax(.cs)` + `App_Start/*` with a minimal-hosting `Program.cs`
  (DI for `SchoolContext`/`NotificationService`, `AddControllersWithViews` with
  `PropertyNamingPolicy = null` to preserve PascalCase JSON for `notifications.js`,
  `UseStaticFiles`/`UseRouting`/default `MapControllerRoute`, best-effort DB init
  guarded by try/catch so the app still starts without a reachable SQL Server).
- Replaced `Web.config` with `appsettings.json` / `appsettings.Development.json`.
  All binding redirects removed (no longer applicable).
- Rewrote `Services/NotificationService.cs` using `System.Threading.Channels`
  (bounded, in-process) in place of MSMQ/`System.Messaging`; preserved the exact
  public surface (`SendNotification`/`ReceiveNotification`/`MarkAsRead`/`Dispose`).
  Registered as a DI singleton.
- Migrated all 6 controllers + `BaseController` from `System.Web.Mvc` to
  `Microsoft.AspNetCore.Mvc` with constructor DI replacing manual construction;
  translated `HttpNotFound()`→`NotFound()`, `HttpStatusCodeResult`→`BadRequest()`,
  `[Bind(Include=...)]`→`[Bind("...")]`, `HttpPostedFileBase`→`IFormFile` +
  `IWebHostEnvironment` (replacing `Server.MapPath`), `TryUpdateModel`→async
  `TryUpdateModelAsync` with expression-based includes, removed
  `JsonRequestBehavior.AllowGet`.
- Moved static assets to `wwwroot/` (`css/`, `js/`), switched Bootstrap to CDN
  5.3.3 (no local bootstrap.css ever existed in the repo), replaced all
  `@Scripts.Render`/`@Styles.Render` bundle calls with direct tags, added
  `Views/_ViewImports.cshtml`. Rewrote `Views/Shared/Error.cshtml` to use
  `ErrorViewModel`/`RequestId` instead of `HandleErrorInfo`/`HttpContext.Current`.
  Moved course teaching-material uploads to `wwwroot/uploads/TeachingMaterials`
  (must be under `wwwroot` to be served by static file middleware) and updated
  `.gitignore` (both copies) accordingly.
- Removed dead code: `Data/SchoolContextFactory.cs` (replaced by DI), empty
  `Services/LoggingService.cs`, `Properties/AssemblyInfo.cs` (SDK auto-generates),
  `Views/Web.config` (Razor-specific, obsolete in Core).
- Build: `dotnet build ContosoUniversity.sln` → **0 Warnings, 0 Errors** (fixed a
  `CS0114` hide-without-`new` warning on `HomeController.Unauthorized()` and the
  NuGet security advisory warnings above).
- Smoke test: ran the app (`dotnet run`), confirmed `/` (200), `/Notifications/GetNotifications`
  (200, correct PascalCase JSON shape). `/Home/About` and `/Students` return 500
  due to `PlatformNotSupportedException: LocalDB is not supported on this platform`
  — expected/environmental (no SQL Server/LocalDB available on this Linux box),
  not a migration defect; EF Core/controller code itself is unchanged in logic.
- Updated documentation (`README.md`, `NOTIFICATION_SYSTEM_README.md`,
  `SETUP_TESTING_GUIDE.md`, `TEACHING_MATERIAL_UPLOAD.md`) to reflect the new
  stack and remove/flag obsolete MSMQ setup instructions.

No automated tests exist in the solution (confirmed at assessment time), so
`generateNewUnitTests: false` / `passUnitTests: true` success criteria are vacuous
(nothing to generate or run) — consistent with the task's stated success criteria.
