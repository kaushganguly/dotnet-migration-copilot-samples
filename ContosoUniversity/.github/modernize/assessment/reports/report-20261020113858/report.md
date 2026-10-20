# ContosoUniversity Assessment

**Report ID:** 20261020113858

> **Assessment limitation:** Core AppCAT and the .NET upgrade analyzer could not load the legacy web project because the Linux SDK installation lacks `Microsoft.WebApplication.targets`. The security scan completed independently. The upgrade assessment output reports zero analyzed projects and zero issues; this is not evidence of upgrade compatibility.

## Application Information

| Property | Value |
|---|---|
| Project | ContosoUniversity |
| Project type | ASP.NET MVC web application |
| Current target | .NET Framework 4.8 |
| Requested target | .NET 10 (`net10.0`) |
| Core assessment | Blocked while loading Visual Studio web targets |

## Summary

| Metric | Count |
|---|---:|
| Total security findings (unique rule/advisory groups) | 4 |
| CVE findings (minimum severity: high) | 1 |
| CWE findings | 3 |
| CWE rules assessed | 59 |
| CWE rules passed (not found) | 56 |
| Upgrade projects analyzed | 0 |
| Upgrade issues reported | 0 |

## Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 

- **ID:** CVE-2024-0056
- **Category:** CVE
- **Severity:** mandatory
- **Story points:** 1
- **Files:** packages.config:14

[CVE-2024-0056](https://github.com/advisories/GHSA-98g6-xh36-x2p7): Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 

Severity: HIGH

Affected dependencies:
  - Microsoft.Data.SqlClient 2.1.4 (declared at packages.config:14; vulnerable range: < 2.1.7)
    Recommended fix: upgrade to 2.1.7
  - Microsoft.Data.SqlClient 2.1.4 (declared at packages.config:14; vulnerable range: >= 3.0.0, < 3.1.5)
    Recommended fix: upgrade to 3.1.5
  - Microsoft.Data.SqlClient 2.1.4 (declared at packages.config:14; vulnerable range: >= 4.0.0, < 4.0.5)
    Recommended fix: upgrade to 4.0.5
  - Microsoft.Data.SqlClient 2.1.4 (declared at packages.config:14; vulnerable range: >= 5.0.0, < 5.1.3)
    Recommended fix: upgrade to 5.1.3

## Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')

- **ID:** CWE-22
- **Category:** File & Path Security
- **Severity:** optional
- **Story points:** 8
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172, Controllers/CoursesController.cs:175

CoursesController.Edit binds TeachingMaterialImagePath from the POST request (line 130), maps the client-supplied path without containment validation (line 172), then deletes the mapped file (line 175). A crafted traversal path can resolve outside the intended TeachingMaterials directory and cause deletion of an unintended file.

## Relative Path Traversal

- **ID:** CWE-23
- **Category:** File & Path Security
- **Severity:** optional
- **Story points:** 5
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172, Controllers/CoursesController.cs:175

The client-controlled TeachingMaterialImagePath in CoursesController.Edit is passed to Server.MapPath and then System.IO.File.Delete with no canonical-path containment check. Relative '..' segments can escape the intended upload directory and direct deletion at another mapped file.

## Improper Control of Resource Identifiers ('Resource Injection')

- **ID:** CWE-99
- **Category:** Injection Attacks
- **Severity:** potential
- **Story points:** 3
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172, Controllers/CoursesController.cs:175

CoursesController.Edit binds TeachingMaterialImagePath from the request, resolves this client-provided identifier with Server.MapPath, and deletes the mapped file. The input is not constrained to the intended upload resource, so a crafted identifier can select a file resource outside the intended scope.

## .NET Upgrade Assessment

- **Target:** .NETCoreApp,Version=v10.0
- **Projects detected by analyzer:** 0
- **Issues reported by analyzer:** 0
- The upgrade scenario artifacts are available in [`scenarios/dotnet-version-upgrade/`](scenarios/dotnet-version-upgrade/).
- The assessment invocation reported the missing `Microsoft.WebApplication.targets` import. Results with zero analyzed projects are inconclusive.

## Codebase Insights

> Codebase insight fact files were not generated because the core AppCAT analysis could not load the project.
