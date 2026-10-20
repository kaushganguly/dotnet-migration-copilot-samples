# Security Assessment Report

**Generated:** 2026-10-20T11:38:58.902409Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 4 |
| CVE Vulnerabilities | 1 |
| CWE Vulnerabilities | 3 |
| Total Rules Assessed | 59 |
| Rules Passed | 56 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 1 |
| optional | 2 |
| potential | 1 |

## CVE Findings (Dependency Vulnerabilities)

### CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 
- **Category:** CVE
- **Severity:** mandatory
- **Story Points:** 1
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


## CWE Findings (Code-Level Vulnerabilities)

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172, Controllers/CoursesController.cs:175

CoursesController.Edit binds TeachingMaterialImagePath from the POST request (line 130), maps the client-supplied path without containment validation (line 172), then deletes the mapped file (line 175). A crafted traversal path can resolve outside the intended TeachingMaterials directory and cause deletion of an unintended file.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172, Controllers/CoursesController.cs:175

The client-controlled TeachingMaterialImagePath in CoursesController.Edit is passed to Server.MapPath and then System.IO.File.Delete with no canonical-path containment check. Relative '..' segments can escape the intended upload directory and direct deletion at another mapped file.

### CWE-99: Improper Control of Resource Identifiers ('Resource Injection')
- **Category:** Injection Attacks
- **Severity:** potential
- **Story Points:** 3
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172, Controllers/CoursesController.cs:175

CoursesController.Edit binds TeachingMaterialImagePath from the request, resolves this client-provided identifier with Server.MapPath, and deletes the mapped file. The input is not constrained to the intended upload resource, so a crafted identifier can select a file resource outside the intended scope.
