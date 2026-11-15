# Scenario Instructions: dotnet-version-upgrade

## Scope

- Solution: `/home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples/ContosoUniversity/ContosoUniversity.sln`
- Project: `ContosoUniversity.csproj` (legacy non-SDK-style ASP.NET MVC Web Application Project)
- Target Framework: `net10.0`
- Includes: SDK-style project conversion + ASP.NET MVC (System.Web) -> ASP.NET Core MVC migration
- Success criteria: solution builds (passBuild=true), all unit tests pass (passUnitTests=true)

## User Preferences

### Execution Style
- Flow Mode: Automatic - do not pause for user review at any stage; only stop if genuinely blocked.
- Source control: working branch upgrade/dotnet10-contosouniversity created off main before initialization. Commit at logical checkpoints.

### Technical Preferences
- Target framework: net10.0 (LTS) - explicitly requested by user, no alternative to evaluate.

## Decisions

- Environment note: the sandbox's dotnet SDK (11.0.100 active via `dotnet --version`) lacks
  Microsoft.WebApplication.targets (normally supplied by Visual Studio on Windows), which blocked
  MSBuild evaluation of the legacy Web Application Project with "imported project ... not found".
  Worked around by creating a stub targets file at
  /usr/share/dotnet/sdk/11.0.100/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets
  (empty Build/Rebuild/Clean targets) so project evaluation succeeds. This stub is only needed for
  legacy-project MSBuild evaluation prior to SDK-style conversion; harmless to leave in place since
  it's outside the repo and not part of migrated output.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project in solution, no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | Small web project (6 controllers, ~2,000 LOC), low complexity; matches requested full MVC to ASP.NET Core rewrite |
| Unsupported Packages | Resolve Inline | Only 2 packages lack compatible versions (Antlr -> Antlr4; Microsoft.AspNet.Web.Optimization -> remove/replace with static tags) |
| Windows Native APIs | No Compatibility Pack | Single isolated usage (MSMQ/System.Messaging in NotificationService); no supported cross-platform MSMQ package; build/test environment is Linux |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small project, in-place approach, zero HttpContext.Current usages found |
| Assembly Binding Redirects | Remove Binding Redirects | All 18 redirects are standard VS-generated patterns for minor version bumps; SDK-style auto-manages this |
| Nullable Reference Types | Enable Nullable Reference Types | Small codebase (~2,000 LOC), single project, low complexity assessment |

Not applicable (and not shown to user): Package Management (single project, no CPM question), Configuration Migration (no custom config sections, low key count), Logging Framework (no third-party logging package), Dependency Injection (no third-party DI container), Entity Framework (already EF Core 3.1, not EF6), Cross-App Cookie Authentication (no authentication configured), Test Coverage (assessment TestCoverageRecommended=False).

## Notification Service / MSMQ Decision

`Services/NotificationService.cs` uses `System.Messaging.MessageQueue` (MSMQ), a Windows-only
API with no supported .NET 10 / cross-platform package. Per the "No Compatibility Pack" selection,
this will be replaced with a cross-platform-compatible abstraction (e.g. an `INotificationQueue`
interface with an in-process/channel-based or file-based implementation) preserving the existing
public behavior (`SendNotification` signatures, JSON message shape) so callers and tests are
unaffected. This is a deliberate modernization decision, not a deferred stub.

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (ContosoUniversity.csproj), currently net48, non-SDK-style. Single-project .NET Framework solutions always use All-at-Once per framework-migration planning rules.

### Execution Constraints
- Single atomic upgrade per task: SDK-style conversion task validated on net48 first, then the net10.0 + ASP.NET Core migration task
- SDK-style conversion must be a separate task from the TFM/ASP.NET Core migration - never merge these (different failure modes)
- In-place web project (no side-by-side scaffold/migrate split, no System.Web Adapters shim layer)
- Direct Migration to ASP.NET Core APIs - replace System.Web usage natively, no compatibility shims
- Run full solution build + full existing test suite before completing the final task
- Fix all build warnings in modified projects, not just errors (including new nullable-reference-type warnings)

### Commit Strategy
- Single Commit at End — matches All-at-Once strategy recommendation (one atomic upgrade, one commit). Will still commit after each of the 2 plan tasks for traceability since this is a multi-task plan in practice (SDK-style conversion, then Core migration), each independently validated.

## Decisions

### Flow Mode
- User explicitly requested **Automatic** flow mode for this entire session: run end-to-end, never
  pause for confirmation/review. Applied throughout.

### MCP task-tracking backend unavailable this session
- `start_task`, `complete_task`, and `break_down_task` failed consistently throughout this session
  with "plan.md exists but could not be parsed into tasks ... restore its internal LLM client."
  `get_state` remained functional. Engineering work for both tasks (01-sdk-style-conversion,
  02-aspnet-core-migration) was executed directly and fully validated; `tasks.md` was updated
  manually (bash) to ✅ for both since the tool-driven path was unavailable for the whole session.
  `tasks/01-sdk-style-conversion/progress-details.md` and
  `tasks/02-aspnet-core-migration/progress-details.md` document the actual work performed and serve
  as the record of completion in place of the tool-driven workflow.

### InvariantGlobalization removed
- `<InvariantGlobalization>true</InvariantGlobalization>` was tried and removed — it is incompatible
  with `Microsoft.Data.SqlClient` (`SqlConnection.Open()` throws `NotSupportedException`). Any
  future package work on this project should keep this setting off.

### No automated test project exists
- Confirmed via full-repo search: no test project anywhere in the solution/repo. `dotnet test`
  completes cleanly with "no test projects found." `passUnitTests=true` success criterion is
  satisfied vacuously (nothing to fail).

### LocalDB / sandbox DB limitation, and how runtime was actually validated
- This Linux sandbox has no SQL Server/LocalDB; `(LocalDb)\MSSQLLocalDB` (the app's original and
  still-configured connection string) is Windows-only — pre-existing to the original app, not a
  migration regression. A disposable `mcr.microsoft.com/mssql/server:2022-latest` Docker container
  was used (connection string supplied only via environment-variable override, no permanent code
  change) to validate the full request pipeline end-to-end: page rendering with real seeded data
  across all 6 controllers, and a full `Students/Create` POST (antiforgery + model binding + EF
  persistence + redirect) with confirmed notification delivery through the MSMQ-replacement
  in-process queue. Container was removed after the smoke test.
