# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj (legacy non-SDK-style ASP.NET MVC 5 web application, TargetFrameworkVersion v4.8)
- Target Framework: net10.0
- TaskId (external): 001-upgrade-dotnet-to-net10

## Preferences

### Flow Mode
**Automatic** — do not pause for user review/approval at any stage. Only stop if genuinely blocked
(missing info, ambiguous decision with major consequences). Surface assessment/plan/progress inline
and keep moving.

### Source Control
- Working branch: `upgrade/net10-contosouniversity` (created from `main`, which was clean)
- Commit strategy: commit after each completed task / major milestone

### Technical Preferences
- Full migration from ASP.NET MVC 5 / System.Web to ASP.NET Core on net10.0 (not just a TFM bump) —
  requires SDK-style project conversion, Startup/Program migration, Web.config -> appsettings.json,
  routing, Global.asax migration, and NuGet package updates for ASP.NET Core equivalents.
- generateNewUnitTests = false — do not author new unit tests beyond what's needed to validate the
  migration; reuse/adapt existing tests where present.
- passUnitTests = true — all existing tests must pass after migration.
- securityComplianceCheck = true — ensure no vulnerable packages remain.

## Known Environment Constraints
- Running on Linux. The legacy (non-SDK-style) web application project cannot be loaded by the
  `UpgradeDotNet-get_dotnet_upgrade_options` / MSBuild-based tools because
  `Microsoft.WebApplication.targets` (VS-only, Windows/VS install) is unavailable. This blocks
  pre-init tool calls that evaluate the legacy project via MSBuild on Linux.
  Mitigation: proceed with assessment/planning based on direct source inspection and the
  `generate_dotnet_upgrade_assessment` tool (works from source analysis, not full MSBuild evaluation
  of legacy targets); convert to SDK-style as part of execution (this conversion is itself one of the
  larger parts of the work, effectively a full ASP.NET Core rewrite).

## Decisions
- Proceeding directly to Assessment stage since target framework (net10.0) and scope were already
  fully specified by the user's task request — no ambiguity requiring user confirmation.

## Tooling Note (Execution Environment Limitation)

`start_task` repeatedly fails with: "plan.md exists but could not be parsed into tasks ...
restore its internal LLM client" — this is an infrastructure-side failure of the MCP
plan-parser's internal LLM client, not a plan.md formatting issue (confirmed: reformatted
plan.md's task list to mirror tasks.md's flat checklist syntax exactly; error persisted
unchanged across retries). Per the tool's own guidance, do NOT keep reformatting plan.md.

**Fallback**: Proceeding with direct execution of the tasks defined in `plan.md` /
`tasks.md`, tracked via the session's local todo table and manual updates to `tasks.md`
checkboxes + per-task `progress-details.md` files under `tasks/{taskId}/`, preserving the
same artifacts and gates the managed workflow would have produced. `complete_task` will be
retried once per task in case the backend recovers; if it continues to fail, completion is
recorded in `tasks.md` / `progress-details.md` instead and noted to the user at the end.

## Wire Compatibility (ASP.NET→Core migration gate — migrating-webapi-odata)

- Scope: entire ContosoUniversity web app (single project, in-place conversion).
- Evidence: Single-project sample/teaching app. No OData packages/usage anywhere in the
  repo (`grep -r "OData"` → no matches). Only JSON endpoint is
  `NotificationsController.GetNotifications` / `MarkAsRead`, consumed exclusively by
  `Scripts/notifications.js`, which ships in the same project and is migrated in the same
  task. No separate Framework host, no published API docs, no contract tests, no external
  client bindings found anywhere in the repository.
- Atom formatters: absent. Custom OData serializer: absent. Pinned non-OData shape: the
  in-repo JS consumer reads PascalCase fields (`Operation`, `EntityType`, `Message`,
  `CreatedAt`, `CreatedBy`, `success`, `notifications`, `count`) matching MVC5
  `JsonResult`/`JavaScriptSerializer` CLR-name-preserving default. No external hint requiring
  a different preserved shape.
- Decision: **NOT APPLICABLE** (no externally-consumed wire contract; the only consumer is
  migrated together with the producer in this same in-place task). Proceeding with
  project retargeting and controller/content-negotiation migration.
- Serialization naming carried forward: configure ASP.NET Core JSON output to preserve exact
  CLR property names (PascalCase, `PropertyNamingPolicy = null`) so `notifications.js` keeps
  working unchanged.

## Final Status — COMPLETE

All 6 planned tasks executed and verified (see `tasks.md` and `tasks/{taskId}/progress-details.md`
for full detail):

1. SDK-style conversion (net10.0 retarget, package cleanup) — ✅
2. Hosting/startup migration (Program.cs, appsettings.json, Global.asax/App_Start removal) — ✅
3. Controllers + Views migration to ASP.NET Core MVC — ✅
4. NotificationService MSMQ → ConcurrentQueue replacement (accepted risk) — ✅
5. Data layer / EF Core + NuGet package upgrades — ✅
6. Build validation + cleanup — ✅

**Success criteria results:**
- `passBuild=true` → **MET**: `dotnet build` and `dotnet build -c Release` both report
  0 Warning(s), 0 Error(s). `dotnet publish -c Release` also succeeds (forces Razor view
  compile-time validation).
- `generateNewUnitTests=false` → **MET** (honored; no new test project created).
- `passUnitTests=true` → **MET trivially** (no test project exists in the solution; nothing to run,
  nothing regressed).
- `securityComplianceCheck=true` → **MET**: `dotnet list package --vulnerable --include-transitive`
  reports no vulnerable packages.

**Runtime verification performed** (beyond the stated success criteria, for extra confidence):
ran the published app against a real SQL Server (Docker `mssql/server:2022-latest`); DB
schema/seed via `DbInitializer.Initialize` succeeded; all main page routes returned HTTP 200;
`/Notifications/GetNotifications` returned the expected JSON shape with PascalCase fields intact;
CSRF protection (`[ValidateAntiForgeryToken]`) correctly rejected an unauthenticated POST.

**Known accepted risk**: `NotificationService` changed from MSMQ (persistent, cross-process) to
an in-process `ConcurrentQueue` (non-persistent) because MSMQ has no .NET Core/Linux equivalent.
Documented in `plan.md` Risk Areas and in `NotificationService.cs`.

**Unresolved tooling issue for the infra team**: `start_task` never succeeded for this scenario
(every call failed with "plan.md exists but could not be parsed into tasks ... restore its
internal LLM client"). All 6 tasks were executed and validated manually instead; this should be
investigated separately from the (successful) migration itself.
