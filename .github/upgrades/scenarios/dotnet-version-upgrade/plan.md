# .NET Version Upgrade Plan

## Overview

**Target**: ContosoUniversity ASP.NET MVC 5 web application — full migration from .NET Framework 4.8 / System.Web to ASP.NET Core on .NET 10.0.
**Scope**: 1 project (standalone solution, no dependencies/dependents), ~2,000 LOC, 7 controllers, 45 NuGet packages (24 need upgrade, 2 incompatible, EF Core 3.1 is EOL).

### Selected Strategy
**All-At-Once** — The single project is upgraded in one coordinated effort (no tiers/phases across projects). SDK-style conversion is kept as a separate task from the TFM/API migration task per .NET Framework migration rules (different failure modes: structural vs. API-surface).
**Rationale**: 1 project, .NET Framework 4.8 → net10.0, full System.Web → ASP.NET Core rewrite required.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph to manage. |
| Project Approach — Web Projects | In-place rewrite | Small web project (7 controllers, ~2000 LOC), full migration required (not dual-hosting). |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages, both with known replacements/migration paths. |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small web project, in-place approach, zero `HttpContext.Current` usages found. |
| Assembly Binding Redirects | Document and Review Before Removing | 12 binding redirect issues present (>10 threshold). |
| Nullable Reference Types | Leave Disabled | Default; enable separately as a future effort. |

## Tasks

### 01-prerequisites-sdk-conversion: Prerequisites & SDK-Style Conversion

Verify the .NET 10 SDK toolchain, then convert `ContosoUniversity.csproj` from the legacy (non-SDK-style, `packages.config`) project format to SDK-style (`Microsoft.NET.Sdk.Web`), converting `packages.config` entries to `PackageReference`. This is a structural change only — it does not change the target framework or fix API-surface issues; those are handled in the next task. Document the 12 binding-redirect issues found in `Web.config` (per "Document and Review" selection) before they are dropped during conversion (binding redirects are auto-managed/unnecessary on SDK-style + modern TFMs).

**Done when**: Project file is SDK-style, binding redirects documented, `packages.config` removed, project still builds on its original target framework.

---

### 02-tfm-upgrade-aspnetcore-migration: Upgrade Target Framework & Migrate System.Web/MVC 5 to ASP.NET Core (net10.0)

Retarget the project to `net10.0` and perform the full migration from `System.Web`/ASP.NET MVC 5 to ASP.NET Core MVC, using the `migrating-aspnet-framework-to-core` orchestration skill (in-place mode) and its satellite skills (routing, validation, model binding, HTTP pipeline/Global.asax, content negotiation as applicable). Scope:
- Replace `Global.asax`/`Global.asax.cs` startup with `Program.cs` minimal hosting model; migrate `RouteConfig`/`BundleConfig`/`FilterConfig` registration
- Convert all Controllers (`BaseController`, `CoursesController`, `DepartmentsController`, `HomeController`, `InstructorsController`, `NotificationsController`, `StudentsController`) and Razor Views to ASP.NET Core MVC APIs
- Replace `System.Web.Optimization` bundling with static asset references (wwwroot + `<link>`/`<script>` tags)
- Migrate `Web.config` (`connectionStrings`, `appSettings`) to `appsettings.json` + `IConfiguration`/Options pattern
- Upgrade `Microsoft.EntityFrameworkCore*` 3.1.32 (EOL) → 10.0.14 and fix any EF Core breaking changes; upgrade `Microsoft.Data.SqlClient` 2.1.4 → 7.1.1 (security vulnerability)
- Replace `Antlr` → `Antlr4` 4.6.6 if still required after migration; drop `Microsoft.AspNet.*` and other packages superseded by the shared framework
- Apply `#skill:migrating-mvc-system-web-adapters` guidance for Direct Migration (no adapters) since `HttpContext.Current` usage is zero
- Migrate `Services/NotificationService.cs` off `System.Messaging` (MSMQ) using `#skill:migrating-to-msmq-messaging` — replace with the modern MSMQ.Messaging-compatible package or an equivalent background-queue approach, preserving notification behavior for Students/Courses/Instructors/Departments
- Fix all resulting compilation errors in a single bounded pass

**Done when**: Solution builds with 0 errors on `net10.0` using `Microsoft.NET.Sdk.Web`; no remaining `System.Web`/MVC5 references.

---

### 03-final-validation: Final Validation

Build the full solution, run the existing test suite (if any exist) and fix failures, verify no remaining security vulnerabilities/deprecated packages, and document any deferred recommendations (e.g., `Microsoft.Identity.Client` replacement, enabling Nullable Reference Types later).

**Done when**: Solution builds with 0 errors/0 warnings, all tests pass, success criteria satisfied (passBuild, passUnitTests, securityComplianceCheck).
