# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Selected Strategy

### Selected Strategy
**All-At-Once** — Single project solution; upgraded in one atomic pass.
**Rationale**: 1 project, legacy ASP.NET MVC 5 / System.Web on net48, no dependency graph to manage.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project in solution — no dependency graph to manage |
| Project Approach | In-place rewrite | Small web project (7 controllers), low complexity, no downstream consumers |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Web.Optimization family), small enough to resolve inline |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | 0 `HttpContext.Current` usages, in-place approach confirmed |
| Assembly Binding Redirects | Remove | Auto-generated VS boilerplate redirects; no real version conflicts beyond normal upgrade deltas |
| Nullable Reference Types | Leave Disabled | Avoid unmanageable warning volume during single-pass migration; can be enabled later |
| Test Coverage | Skip | No existing test project; opt-in default; Low assessment difficulty |

## Project

- **ContosoUniversity.csproj** (net48, non-SDK-style, packages.config, ASP.NET MVC 5 / System.Web) → **net10.0, SDK-style, PackageReference, ASP.NET Core**

## Tasks

1. **Prerequisites & SDK-style conversion** — Verify .NET 10 SDK; convert `ContosoUniversity.csproj` from legacy format to SDK-style targeting `net10.0`, replacing `packages.config` with `PackageReference`.
2. **ASP.NET Core migration (in-place)** — Replace System.Web/MVC5 hosting with ASP.NET Core: `Program.cs` minimal hosting, endpoint routing (replacing `RouteConfig.cs`), controllers/views migration, `Web.config` → `appsettings.json`, `Global.asax` cleanup, static files (`Content`/`Scripts` → `wwwroot`), remove `System.Web.Optimization` bundling in favor of static references.
3. **Package & dependency updates** — Update EF Core, Microsoft.Data.SqlClient, Newtonsoft.Json, Microsoft.Extensions.* to net10.0-compatible versions; remove Framework-only/incompatible packages (Web.Optimization, WebGrease, Antlr, MVC5/Razor/WebPages which are now part of the framework reference).
4. **Final validation** — Build solution with 0 errors/warnings; run any available tests; document residual follow-ups (e.g., optional nullable reference types enablement).

*(Single-project, low-complexity solution — tasks 1–3 are executed as one bounded unit per the All-at-Once adaptation rule for very small solutions; task 4 validates the result.)*
