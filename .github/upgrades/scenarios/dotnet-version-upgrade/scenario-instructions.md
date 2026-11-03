# Scenario Instructions: dotnet-version-upgrade

## Source Control
- Repo is a git repository
- Working branch: `upgrade/dotnet10-contosouniversity` (created from `main`)
- Commit Strategy: Single Commit at End (All-at-Once strategy default)

## Preferences
### Flow Mode
**Automatic** — run end-to-end, only pause when genuinely blocked.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages: Antlr, Microsoft.AspNet.Web.Optimization)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects
- Nullable Reference Types: Leave Disabled

## Strategy
**Selected**: All-At-Once
**Rationale**: Single project solution (ContosoUniversity.csproj only) targeting .NET Framework 4.8 → .NET 10.0. Per scenario planning rules, single .NET Framework projects always use All-at-Once regardless of general strategy matrix.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion, TFM upgrade, package updates, and ASP.NET Core migration happen as part of one project scope
- Validate full solution build after upgrade (single bounded pass; fix all compilation errors found)
- Testing occurs after the atomic upgrade completes successfully
- In-place web rewrite — no side-by-side scaffold/YARP proxy needed
- Direct Migration to ASP.NET Core APIs — no System.Web Adapters compatibility shims; replace System.Web usage natively

## Execution Notes (post-completion)

- **MCP task-tracking tools unavailable this session**: `AppModUpgrade-start_task` failed consistently
  with "plan.md exists but could not be parsed into tasks... restore its internal LLM client" — an
  internal backend LLM-client failure in the Upgrade MCP server, not a plan-format issue (confirmed by
  retrying across the whole session, including after task completion). Retried before finalizing;
  still failing. All 4 tasks in `plan.md`/`tasks.md` were executed and validated manually; `tasks.md`
  checkboxes were updated by hand to reflect actual (verified) completion since `complete_task` could
  not be invoked successfully.
- **Final validation performed**: `dotnet build` (both `.csproj` and `.sln`) succeeds with 0
  warnings / 0 errors on `net10.0`. `dotnet list package --vulnerable --include-transitive` reports no
  vulnerable packages. A full HTTP smoke test (temporary local SQLite swap, not committed) confirmed
  all controllers/views render (200 OK) for Index/Details/Create/Edit across Students, Courses,
  Instructors, Departments, Notifications, plus a POST Create flow (antiforgery token) and a file
  upload flow (Course teaching-material image) both completed successfully end-to-end.
- **No test projects exist** in this repository (solution contains only `ContosoUniversity.csproj`),
  so "passUnitTests" has no automated tests to run; build + the manual HTTP smoke-test above are the
  validation evidence for functional correctness instead.
- **Known limitation — MSMQ is Windows-only at runtime**: `Services/NotificationService.cs` was
  migrated from `System.Messaging` to the `MSMQ.Messaging` NuGet package (same API, new namespace).
  Message Queuing itself is a Windows OS feature; on Linux/macOS hosts (including this one) MSMQ is
  not installed, so notification send/receive will fail at runtime (gracefully, via existing
  try/catch) while every other feature of the app works normally. The queue connection was changed
  to open lazily (on first use) rather than in the constructor, specifically so this limitation only
  affects the notification endpoints and does not block any other controller/page.
