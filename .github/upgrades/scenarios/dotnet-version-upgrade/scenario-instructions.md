# Scenario Instructions — dotnet-version-upgrade

## Scope
- Solution: `/home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples/ContosoUniversity/ContosoUniversity.sln`
- Project: `ContosoUniversity.csproj` (ASP.NET MVC 5 / System.Web, .NET Framework 4.8)
- Target Framework: **net10.0**

## Task Mapping
- External TaskId: `001-upgrade-dotnet-to-net10`
- Description: Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10.0
- Requirements: SDK-style project conversion, packages.config -> PackageReference, System.Web MVC 5 -> ASP.NET Core hosting, EF Core/SqlClient/Microsoft.Extensions package updates, resolve API compatibility issues.
- Success Criteria: passBuild=true, generateNewUnitTests=false, passUnitTests=true, securityComplianceCheck=true

## User Preferences

### Flow Mode
**Automatic** - run end-to-end, do not pause for user review/approval at any stage. Only stop if genuinely blocked (missing info, unresolved ambiguity with major consequences, or unrecoverable error). Always print artifact paths inline and keep moving.

### Technical Preferences
- Target framework: net10.0 (LTS)
- Convert legacy csproj to SDK-style as part of this upgrade
- Migrate packages.config to PackageReference
- Migrate System.Web/ASP.NET MVC 5 hosting to ASP.NET Core hosting
- Do not generate new unit tests (generateNewUnitTests=false) - focus on making existing tests pass
- Must pass build and existing unit tests
- Must address security compliance (no vulnerable package versions)

### Execution Style
- No pauses for review; proceed through assessment -> planning -> execution automatically
- Fix all build warnings in touched projects
- Never suppress warnings without explicit approval (none given - so don't suppress)

## Source Control
- Git repo: yes
- Working branch: `upgrade/contosouniversity-net10` (created from `main`, clean tree before start)
- Commit strategy: After Each Task (safer than strategy default of Single Commit at End, given
  the scope includes a full System.Web MVC 5 -> ASP.NET Core in-place rewrite; no explicit user
  preference overrides this)

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages: Antlr, Microsoft.AspNet.Web.Optimization)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects

### Reliability
- Test Coverage: Skip (per explicit success criteria generateNewUnitTests=false; also matches default)

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (ContosoUniversity.csproj) on .NET Framework 4.8 targeting net10.0 - no dependency graph to manage, single atomic upgrade pass.

### Execution Constraints
- Single atomic upgrade - SDK-style conversion, TFM/package upgrade, and System.Web -> ASP.NET Core in-place rewrite all happen within this one project
- Validate full solution build after the upgrade completes (bounded single pass - fix all compile errors, not iterative rebuild/fix cycles)
- No existing unit test project in the solution - passUnitTests is satisfied vacuously (nothing to run); do not generate new tests (generateNewUnitTests=false)
- No tier ordering / no phasing - this is a single-project solution

## Known Environment Limitation
- `start_task`/`complete_task` MCP task-state tooling is non-functional in this environment:
  the internal plan parser depends on MCP sampling back to the host LLM client, which
  consistently returns "The user cancelled the request" (see
  /tmp/UA/logs/*-agent.log, LLMPlanParseResult deserialization failures). This is an
  infrastructure issue, not a plan.md formatting problem.
- Proceeding to execute the plan directly (per plan.md task breakdown), tracking progress
  manually via `tasks.md` checkbox updates and this file, since the automated task-state
  tools cannot be used to drive execution in this session.

## Task 02 Execution Notes (completed)
- `Html.ActionLink` is natively available in ASP.NET Core via
  `Microsoft.AspNetCore.Mvc.Rendering.HtmlHelperLinkExtensions` - all overloads used in this
  codebase (2/3/4/5-arg) are supported out of the box. No shim/reimplementation needed (one
  was written, caused CS0121 ambiguous-call errors, and was deleted).
- `Content/bootstrap.css`/`bootstrap.min.css` were referenced by the original project but never
  physically present in the repo (pre-existing gap from the legacy NuGet "content/" convention).
  Recovered from the NuGet package cache (`bootstrap` 5.3.3) into `wwwroot/Content/` to preserve
  intended styling.
- `System.Messaging` (MSMQ) replaced with the `MSMQ.Messaging` NuGet package (v1.0.4, namespace
  `MSMQ.Messaging`) - verified drop-in API match for `MessageQueue`, `Message`,
  `XmlMessageFormatter`, `MessageQueueAccessRights`, `MessageQueueErrorCode`,
  `MessageQueueException`. MSMQ itself remains a Windows-only runtime feature (same as the
  original app) - on Linux, constructing `NotificationService` throws
  "Message Queuing has not been installed on this computer". This is an environment/platform
  limitation identical in nature to the original app's implicit Windows/IIS hosting assumption,
  not a migration defect.
- Two transitive-dependency security advisories were resolved by pinning direct
  PackageReferences: `System.Drawing.Common` -> `10.0.13` (pulled in old/vulnerable version via
  MSMQ.Messaging) and `System.Security.Cryptography.Pkcs` -> `10.0.13` (pulled in via
  Microsoft.Data.SqlClient). `dotnet list package --vulnerable --include-transitive` and
  `dotnet list package --deprecated` both report clean after these pins.
- `Content/`, `Scripts/`, `Uploads/` moved into `wwwroot/` (git mv) - preserves all relative URLs
  unchanged since ASP.NET Core serves wwwroot at the site root.
- Runtime smoke test performed against a temporary local SQL Server 2022 Docker container
  (removed after testing, not part of the deliverable): EF Core `DbInitializer` seeding and
  app/DI startup succeeded against a real SQL Server; `/Content/...` and `/Scripts/...` static
  assets served 200. Controller actions could not be smoke-tested end-to-end on this Linux
  sandbox because every controller depends on `NotificationService` (via `BaseController`,
  matching the original design) which requires the Windows-only MSMQ runtime - this is a
  pre-existing architectural constraint carried over unchanged from the original app, not a
  migration regression. The default `appsettings.json` connection string intentionally still
  targets LocalDB (Windows-only), matching the original app's implicit hosting assumption.
- Final solution-wide `dotnet build -c Release`: 0 errors, 0 warnings. No test projects exist in
  the repo (`passUnitTests` satisfied vacuously, consistent with `generateNewUnitTests=false`).
