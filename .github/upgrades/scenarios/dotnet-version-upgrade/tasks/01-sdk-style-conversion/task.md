# Task 01: Convert ContosoUniversity project to SDK-style format

## Note on tooling
`start_task`/`complete_task` are unavailable for this scenario: the Upgrade MCP server's
plan-parser reports an internal LLM-client failure ("plan.md exists but could not be parsed
into tasks... restore its internal LLM client"). This is an infrastructure-level issue, not a
plan.md formatting problem (format follows the scenario's canonical template exactly). Per the
tool's own guidance ("Do not regenerate or reformat the plan solely in response to this error"),
execution proceeds directly from plan.md as the authoritative task definition, using
`convert_project_to_sdk_style` and manual research/execution/validation, with progress tracked
here and in progress-details.md instead of via complete_task.

## Objective
Convert `ContosoUniversity.csproj` from legacy (non-SDK-style) to SDK-style format while
remaining on `net48` (no TFM change in this task). Migrate `packages.config` to
`<PackageReference>` items. Remove `packages.config`.

## Research
- Current csproj: `ToolsVersion="15.0"`, `ProjectTypeGuids` include Web Application Project
  GUID (`{349c5851-65df-11da-9384-00065b846f21}`), `TargetFrameworkVersion` v4.8,
  `OutputType` Library (web app project model, no explicit Main).
- `packages.config` has 40 packages (many are transitive dependencies of EF Core 3.1 /
  Microsoft.Extensions.* 3.1.32 that will collapse once modern PackageReference resolves
  transitive deps automatically).
- Tool to use: `convert_project_to_sdk_style` (dedicated MCP tool, not the broken plan-parser).
