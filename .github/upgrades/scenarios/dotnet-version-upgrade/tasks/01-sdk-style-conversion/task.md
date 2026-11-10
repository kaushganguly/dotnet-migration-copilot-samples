# Task 01: Convert ContosoUniversity to SDK-style project format

(See plan.md for original task description. This file is the working/research memory —
manually maintained because `start_task` cannot parse plan.md in this environment;
see "Known Environment Limitation" in scenario-instructions.md.)

## Research

- Legacy csproj: `ToolsVersion="15.0"`, imports `Microsoft.WebApplication.targets` via
  `$(VSToolsPath)`, `TargetFrameworkVersion=v4.8`, `OutputType=Library`,
  `ProjectTypeGuids` include MVC web app type guid `{349c5851-...}`.
- `packages.config` has 45 packages — all must become `PackageReference` with
  identical versions (format-only conversion, no version bumps here).
- Content items (Views/*.cshtml, Content/*, Scripts/*, Uploads/*) are currently explicit
  `<Content Include>` entries — SDK-style implicit globbing will pick these up; watch for
  the conversion tool's "excluded from globbing" markers.
- No other projects in the solution (`ContosoUniversity.sln` has exactly 1 project) —
  conversion order is trivial (single project, no topological ordering needed).
- Hard constraint: do NOT change `TargetFrameworkVersion`/`TargetFramework` in this task —
  stays on `net48`-equivalent until task 02. Do NOT bump package versions here.

## Tool

Use `convert_project_to_sdk_style(solutionPath, projectPath)` (now unblocked after placing
`Microsoft.WebApplication.targets` at the expected SDK path — see scenario-instructions.md
Decisions).

## Done when
- csproj is SDK-style, packages.config removed, all packages as PackageReference,
  TFM unchanged (still net48-equivalent).
- Project loads without MSBuild errors.
