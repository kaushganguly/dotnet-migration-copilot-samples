# Security Assessment Report

**Generated:** 2026-10-20T11:14:37.176269Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 5 |
| CVE Vulnerabilities | 1 |
| CWE Vulnerabilities | 4 |
| Total Rules Assessed | 59 |
| Rules Passed | 55 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 1 |
| optional | 4 |
| potential | 0 |

## CVE Findings (Dependency Vulnerabilities)

### CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** packages.config:14

[CVE-2024-0056](https://github.com/advisories/GHSA-98g6-xh36-x2p7): Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 

Severity: HIGH

Affected dependencies:
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14); first patched version: 2.1.7
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14); first patched version: 3.1.5
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14); first patched version: 4.0.5
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14); first patched version: 5.1.3

Microsoft.Data.SqlClient and System.Data.SqlClient SQL Data Provider Security Feature Bypass Vulnerability

## CWE Findings (Code-Level Vulnerabilities)

### CWE-732: Incorrect Permission Assignment for Critical Resource
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** Services/NotificationService.cs:20-23

NotificationService creates the message queue and grants MessageQueueAccessRights.FullControl to the Everyone principal, allowing unintended actors to read or modify the security-critical queue.

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** Controllers/CoursesController.cs:47, Controllers/CoursesController.cs:172-175, Controllers/CoursesController.cs:227-235

The Create and Edit actions bind TeachingMaterialImagePath from the request. Edit and DeleteConfirmed pass that value to Server.MapPath and delete the resolved file without constraining it to Uploads/TeachingMaterials. A crafted path can therefore escape the intended upload directory and cause deletion outside it.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** Controllers/CoursesController.cs:47, Controllers/CoursesController.cs:172-175, Controllers/CoursesController.cs:227-235

The request-bound TeachingMaterialImagePath is mapped and then used in file deletion without rejecting or resolving relative traversal sequences such as ../, allowing a crafted relative path to escape the upload directory.

### CWE-36: Absolute Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** Controllers/CoursesController.cs:47, Controllers/CoursesController.cs:172-175, Controllers/CoursesController.cs:227-235

The request-bound TeachingMaterialImagePath is passed to Server.MapPath and then used for file deletion without enforcing an upload-directory root, so absolute virtual paths can resolve outside the intended directory.
