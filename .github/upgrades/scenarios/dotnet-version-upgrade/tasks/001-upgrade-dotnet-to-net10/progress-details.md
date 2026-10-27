# Progress — 001-upgrade-dotnet-to-net10 (parent task)

## Summary
Upgraded `ContosoUniversity` from ASP.NET MVC 5 / .NET Framework 4.8 to ASP.NET Core MVC on
.NET 10.0, via 5 subtasks:

1. **001.01-project-conversion** — `ContosoUniversity.csproj` rewritten as SDK-style
   (`Microsoft.NET.Sdk.Web`, `net10.0`); `packages.config`/`AssemblyInfo.cs` removed;
   `PackageReference`s upgraded (`Microsoft.EntityFrameworkCore*` 10.0.14,
   `Microsoft.Data.SqlClient` 7.1.1, `Newtonsoft.Json` 13.0.4, plus an explicit
   `System.Security.Cryptography.Pkcs` 10.0.14 pin to patch a transitive vulnerability).
2. **001.02-hosting-startup-migration** — added `Program.cs` (minimal hosting model),
   `appsettings.json`/`appsettings.Development.json`; removed `Global.asax(.cs)`,
   `App_Start/*`, `Web.config`, `Views/Web.config`; moved `Content/`, `Scripts/`, `Uploads/`
   under `wwwroot/`.
3. **001.03-controllers-services-migration** — ported `BaseController` + all 6 controllers to
   `Microsoft.AspNetCore.Mvc` (DI constructors, `NotFound()`/`BadRequest()`, `[Bind("...")]`,
   `IFormFile`/`IWebHostEnvironment`, `TryUpdateModelAsync`); rewrote `NotificationService`
   to use an in-process `ConcurrentQueue` instead of MSMQ (`System.Messaging`, which has no
   .NET 10 equivalent); replaced `SchoolContextFactory` with an EF Core design-time factory.
4. **001.04-views-configuration-migration** — added `Views/_ViewImports.cshtml`; replaced
   `@Styles.Render`/`@Scripts.Render` bundling in `_Layout.cshtml` and 8 Create/Edit views
   with direct `<link>`/`<script>` tags; migrated `Error.cshtml` to `ErrorViewModel`.
5. **001.05-build-and-test-validation** — fixed 2 build issues (missing `using`s in
   `Program.cs`, `CS0114` hide-warning on `HomeController.Unauthorized()`); confirmed clean
   `dotnet build` (0 errors/0 warnings); removed the temporary SDK-targets stub used only
   during assessment tooling; updated `.gitignore` for the new `wwwroot/Uploads` path;
   documented that `passUnitTests=true` is satisfied vacuously (no test project exists) and
   that full DB-backed runtime verification isn't possible in this Linux sandbox (no SQL
   Server/LocalDB), which is an environment limitation rather than a migration regression.

## Success Criteria
- **passBuild=true** → met (`dotnet build` succeeds, 0 errors, 0 warnings).
- **passUnitTests=true** → met vacuously (no test project in the repository/solution).

## Key documented trade-offs
- `NotificationService` no longer persists notifications across process restarts (was
  MSMQ-backed; now an in-memory `ConcurrentQueue` singleton) — MSMQ has no .NET 10 / Linux
  equivalent.
- JSON responses from `/Notifications/GetNotifications` are kept PascalCase
  (`JsonSerializerOptions.PropertyNamingPolicy = null`) to preserve compatibility with the
  unmodified `wwwroot/Scripts/notifications.js` client code.
- `~/Content/bootstrap.css` was referenced by the old bundle config but the file never
  existed in the repository; this dangling reference was dropped rather than fabricated.
