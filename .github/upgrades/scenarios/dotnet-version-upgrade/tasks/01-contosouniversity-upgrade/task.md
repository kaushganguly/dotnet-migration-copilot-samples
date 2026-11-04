# 01-contosouniversity-upgrade: Upgrade ContosoUniversity to .NET 10.0

> NOTE: `start_task` could not be used — the MCP plan-parser's internal LLM call failed
> deterministically (MCP sampling returns "The user cancelled the request" in this CLI host,
> confirmed in /tmp/UA/logs). This task.md was written by hand from plan.md as a substitute,
> preserving the same scope/content, so execution can still be tracked.

(See plan.md task 01-contosouniversity-upgrade for full description, scope, risks, done-when criteria.)

## Research notes (filled in during execution)
- Project: ContosoUniversity/ContosoUniversity.csproj — already converted to SDK-style via
  `convert_project_to_sdk_style` (still targeting net48 at this point).
- Current package set / versions: see assessment/nuget/aggregate-packages.md and
  assessment/projects/ContosoUniversity.md.
- Environment note: this Linux CI environment lacks Visual Studio's
  Microsoft.WebApplication.targets; a stub was created at
  /usr/share/dotnet/sdk/10.0.403/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets
  so MSBuild evaluation of the legacy project succeeds for tooling purposes.
