# Upgrade Plan: ContosoUniversity → .NET 10.0

### Selected Strategy
**All-At-Once** — All projects (1 project: ContosoUniversity.csproj) upgraded simultaneously in a single operation.
**Rationale**: Single-project solution on .NET Framework 4.8. Per scenario planning rules, single .NET Framework projects always use All-at-Once regardless of the general strategy matrix.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project solution — no tiering needed |
| Project Approach | In-place rewrite | Small web project (≤10 controllers, 1983 LOC, Low difficulty per assessment); no CI-green/incremental-deploy constraint stated |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr, Microsoft.AspNet.Web.Optimization) — small enough to research and resolve within the same task |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach confirmed; small web project with low `HttpContext.Current` usage — no compatibility shim layer needed |
| Assembly Binding Redirects | Remove Binding Redirects | .NET Core/.NET 10 does not use binding redirects; assembly resolution is handled differently |
| Nullable Reference Types | Leave Disabled | Not explicitly requested; keep scope focused on the TFM/stack migration, enable separately later if desired |

## Project List

- **ContosoUniversity.csproj** (web application, MVC 5 / System.Web, non-SDK-style, `packages.config`) → target `net10.0`, SDK-style, ASP.NET Core MVC, `PackageReference`

## Tasks

### 01-prerequisites-and-sdk-style-conversion: Verify toolchain and convert to SDK-style format

Verify the .NET 10 SDK is installed and compatible, then convert `ContosoUniversity.csproj` from the legacy (non-SDK-style) project format to SDK-style, migrating `packages.config` to `PackageReference` as part of the conversion. This is a structural change only — the project remains on `net48` / `v4.8` after this task; no TFM or API changes happen here. The assessment flags the project as `Sdk Style = False` with 41 package issues and 12 binding-redirect issues already surfaced — those are addressed in later tasks, not here.

Research starting points: review `ContosoUniversity.csproj` for `ItemGroup`/`Reference`/`Content`/`Compile` items that need SDK-style globbing adjustments, review `packages.config` for the full package list, and confirm the `.sln` still loads correctly post-conversion.

**Done when**: `ContosoUniversity.csproj` is SDK-style, `packages.config` is removed, all packages are declared via `PackageReference`, and the project still targets `net48` (building with the legacy framework reference set, pre-TFM-upgrade) without introducing new errors beyond what the conversion tool reports as expected.

### 02-upgrade-to-net10-and-packages: Upgrade TFM, packages, and fix compilation errors

Upgrade `ContosoUniversity.csproj`'s target framework from `net48` to `net10.0`, update all NuGet package references for .NET 10 compatibility (including the security-vulnerable `Microsoft.Data.SqlClient` 2.1.4 → 7.1.1, and EF Core / Microsoft.Extensions.* packages 3.1.32 → 10.0.14), remove binding redirects (no longer needed on modern .NET), resolve the 2 incompatible packages inline (replace `Antlr` 3.4.1.9004 with `Antlr4` 4.6.6; remove `Microsoft.AspNet.Web.Optimization` — replace bundling/minification with direct `<script>`/`<link>` tags or a modern bundler), and fix all resulting compilation errors in a single bounded pass.

Known risks from the assessment: 41 package issues, 12 binding-redirect issues (manual redirects conflicting with auto-generated ones, and redirects forcing version downgrades — removed as part of this task), `System.Web.Optimization` bundling (Feature.0001) which has no .NET Core equivalent, and packages whose functionality moves to framework references (Microsoft.AspNet.Mvc/Razor/WebPages, Microsoft.Web.Infrastructure, System.Buffers/Memory/etc.) which should be removed rather than upgraded.

Research starting points: inventory all `<PackageReference>` entries after Task 01, check EF Core DbContext/configuration code for 3.1 → 10 behavior changes, and check `Global.asax.cs` / `App_Start` for code that depends on packages being removed.

**Done when**: the project targets `net10.0`, all package references are updated or replaced per the assessment, no binding redirects remain, and the solution builds (even if ASP.NET Core migration work in Task 03 is still pending for runtime behavior).

### 03-migrate-mvc5-to-aspnetcore: Migrate ASP.NET MVC 5 (System.Web) to ASP.NET Core

Migrate the System.Web-based ASP.NET MVC 5 application to ASP.NET Core MVC running on .NET 10: replace `Global.asax`/`Global.asax.cs` application startup with `Program.cs` (minimal hosting model), convert `RouteConfig.cs`/`RouteCollection` routing to endpoint routing (`MapControllerRoute`), migrate controllers/views/filters to ASP.NET Core equivalents, convert `web.config` settings (`appSettings`, `connectionStrings`, custom sections) to `appsettings.json` / `IConfiguration`, replace `System.Web.Optimization` bundling with direct static asset references (`wwwroot`), and replace any remaining `System.Web` API usage (`HttpContext.Current`, `Server.MapPath`, etc.) directly with ASP.NET Core APIs per the confirmed "Direct Migration" option (no compatibility adapters).

Known risks from the assessment: Feature.0001 (System.Web.Optimization bundling/minification must be replaced with actual asset tags), Feature.1000 (Global.asax.cs application-initialization code must be converted and cleaned up). Since Project Approach = In-place rewrite, this is a full in-place replacement — no side-by-side scaffold/YARP proxy is used, and the old System.Web pipeline is retired in the same pass.

Research starting points: inventory `Controllers/`, `Views/`, `App_Start/` (RouteConfig, BundleConfig, FilterConfig), `Global.asax.cs`, `Web.config`, and `Services/` for any System.Web dependencies; check `Models/` and `Data/` for EF Core DbContext initialization patterns that need updating for the hosting model change; check for Forms/session/authentication usage that needs ASP.NET Core Identity/cookie auth equivalents.

**Done when**: the application runs on the ASP.NET Core hosting model (`Program.cs`), all controllers/views function under endpoint routing, `Global.asax`/`web.config` legacy constructs are removed or converted, and no remaining compile-time or obvious runtime dependency on `System.Web` assemblies exists outside of necessary interop.

### 04-final-validation: Build, test, and document deferred recommendations

Run a full solution build and fix any remaining compilation errors. Run the full test suite (if test projects exist) and fix failing tests. Run a security compliance check (confirm `Microsoft.Data.SqlClient` and other flagged-vulnerable packages are updated to non-vulnerable versions, and that `Microsoft.Identity.Client` — flagged deprecated — is either upgraded or documented as a deferred recommendation if no safe in-place replacement exists). Document any deferred/optional recommendations (e.g., Nullable Reference Types left disabled per the confirmed upgrade option) for the user.

**Done when**: the solution builds with 0 errors and 0 warnings in modified projects, all tests pass (or test absence is documented), and the security vulnerability in `Microsoft.Data.SqlClient` is resolved.
