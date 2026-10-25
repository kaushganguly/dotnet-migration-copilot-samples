# Scenario Instructions: dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj (ASP.NET MVC 5, legacy non-SDK-style, net48, System.Web)
- Target framework: net10.0 (LTS)

## User Preferences

### Flow Mode
- **Automatic** — do not pause for user input/review at any stage. Only stop if genuinely blocked.

### Technical Preferences
- Target framework: net10.0
- Convert project to SDK-style format as part of the upgrade.
- Project uses System.Web/ASP.NET MVC 5 (not already ASP.NET Core); migrating to net10.0 requires migrating off System.Web to ASP.NET Core equivalents (Microsoft.AspNetCore.* / Kestrel hosting model) as part of this upgrade.

## Environment Notes
- Linux build agent: `/usr/share/dotnet/sdk/10.0.402/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets` did not exist (no Visual Studio installed) and blocked all MSBuild evaluation of the legacy web project. Created a minimal stub targets file at that path so MSBuild can evaluate the legacy project before SDK-style conversion. This stub is environment-level (outside the repo) and not part of the repo diff.

## Decisions
- Working branch `upgrade/dotnet10-contosouniversity` created from `main` (clean tree) before `initialize_scenario`.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Project); System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages, both have known replacements: Microsoft.AspNet.Web.Optimization → static tag references; Microsoft.Data.SqlClient → upgrade to 7.1.1)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (6 controllers, ~1979 LOC, 0 HttpContext.Current usages, no HttpModule/HttpHandler — low complexity, in-place approach)

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects (target net10.0 does not use assemblyBinding at all; all referenced packages upgraded to compatible versions)
- Nullable Reference Types: Leave Disabled (no explicit team request for nullable from day one; keep migration scope focused)

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (ContosoUniversity.csproj only) targeting net48 — no dependency graph to manage, so no tiered/bottom-up approach is needed. The ASP.NET MVC 5 web project is small (6 controllers, ~1979 LOC, 🟢 Low assessed difficulty) so it is migrated in-place directly to ASP.NET Core on .NET 10 rather than side-by-side.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion, TFM retarget to net10.0, System.Web → ASP.NET Core migration, and package upgrades all happen together in one pass
- Validate full solution build (and tests) after the upgrade is applied
- Migrate System.Messaging (MSMQ) usage in Services/NotificationService.cs — not supported on modern .NET; needs the MSMQ.Messaging package per migrating-to-msmq-messaging skill
- No old project kept side-by-side — Global.asax/App_Start/System.Web artifacts are removed as part of this same task, not deferred

## Source Control
- Working branch: upgrade/dotnet10-contosouniversity (created from main, clean tree)
- Commit Strategy: Single Commit at End (matches All-at-Once strategy — one atomic upgrade, one commit)

## Tool Incident: start_task plan-parser failure
`start_task('01-prerequisites-sdk-style-conversion')` consistently returned:
"Cannot start ... plan.md exists but could not be parsed into tasks ... restore its internal LLM client"
— reproduced across multiple retries (including after simplifying plan.md to pure prose paragraphs,
ruling out a content/formatting cause) and after waits up to several minutes. This is a backend
service fault in the Upgrade MCP's plan-parser, outside agent control.

Per Automatic flow mode and the explicit directive to complete the upgrade end-to-end without
pausing, execution proceeded directly against plan.md's task list (3 tasks) without the
start_task/complete_task state tracking calls succeeding. Progress is recorded manually in
`tasks/{taskId}/task.md` and `tasks/{taskId}/progress-details.md`, and `tasks.md` checkboxes are
updated by hand to mirror actual completion state, preserving the same artifact contract the
tools would have produced. complete_task will be retried once the service recovers; if it
succeeds retroactively, state will reconcile with what's on disk.

## Final Status (2025, Automatic mode)

**Scenario complete — all 3 tasks done.**

- Task 01 (SDK-style conversion): ✅ done
- Task 02 (System.Web → ASP.NET Core, retarget net10.0): ✅ done
- Task 03 (final validation): ✅ done — `complete_task` succeeded normally (backend plan-parser
  fault from task 01/02 had recovered by this point).

**Success criteria:**
- passBuild=true: ✅ Debug + Release builds, 0 warnings / 0 errors, from a clean `bin`/`obj` state.
- passUnitTests=true: ✅ No test projects exist in the repo (`discover_test_projects` → `[]`);
  vacuously satisfied.

**Verified no vulnerable NuGet packages remain** (`dotnet list package --vulnerable` → none),
despite the `postCompletion.detectedSignals` field on the final `complete_task` response still
showing stale pre-migration baseline signals (HasPackageVulnerabilities,
HasLegacyProjectFormat, HasLegacyFramework) — these reflect the original assessment snapshot,
not the current state of the repo.

See task 02's `task.md`/`progress-details.md` and task 03's `progress-details.md` for full
detail, the wire-compatibility gate record, and deferred/follow-up recommendations (LocalDB/
MSMQ require a Windows host to verify at runtime; no test project exists yet; a couple of
already-dead bootstrap.css references were not carried forward).

Final commit: single commit containing the entire migration diff, per the confirmed Source
Control / Commit Strategy.
