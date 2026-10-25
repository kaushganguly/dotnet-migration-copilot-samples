# Task 01: Verify toolchain and convert ContosoUniversity to SDK-style format

(Manually created — start_task tool unavailable due to plan-parser infra failure; see
scenario-instructions.md > Tool Incident. Content mirrors plan.md task 01.)

## Objective
Verify the .NET 10 SDK is installed and compatible, then convert ContosoUniversity.csproj
from the legacy non-SDK-style project format to SDK-style format, while staying on net48
for this task (structural change only, no TFM change).

## Research notes
- Current csproj: ToolsVersion="15.0", ProjectTypeGuids for MVC/Web Application flavors,
  TargetFrameworkVersion v4.8, packages.config with 45 packages, legacy imports
  (Microsoft.CSharp.targets, Microsoft.WebApplication.targets).
- Environment note: this sandbox has no Visual Studio; Microsoft.WebApplication.targets
  was stubbed at /usr/share/dotnet/sdk/10.0.402/Microsoft/VisualStudio/v18.0/WebApplications/
  to allow MSBuild evaluation (see scenario-instructions.md > Environment Notes). That
  import is removed entirely once SDK-style, so this only matters before conversion.
- .NET 10 SDK confirmed installed (validate_dotnet_sdk_installation → Compatible SDK found).
- Convert packages.config → PackageReference as part of SDK-style conversion.
- Relevant tool: convert_project_to_sdk_style(solutionPath, projectPath).

## Done when
ContosoUniversity.csproj is SDK-style, still targets net48, uses PackageReference instead
of packages.config, and the project loads/evaluates cleanly with dotnet build.
