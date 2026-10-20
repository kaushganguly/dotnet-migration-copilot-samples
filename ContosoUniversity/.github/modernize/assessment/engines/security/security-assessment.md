# Security Assessment Report

**Generated:** 2026-10-20T12:35:19.034291Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 8 |
| CVE Vulnerabilities | 1 |
| CWE Vulnerabilities | 7 |
| Total Rules Assessed | 59 |
| Rules Passed | 52 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 3 |
| optional | 4 |
| potential | 1 |

## CVE Findings (Dependency Vulnerabilities)

### CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** packages.config:14

[CVE-2024-0056](https://github.com/advisories/GHSA-98g6-xh36-x2p7): Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 

Severity: HIGH

Affected dependencies:
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14; vulnerable range < 2.1.7; first patched version not specified)
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14; vulnerable range >= 3.0.0, < 3.1.5; first patched version not specified)
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14; vulnerable range >= 4.0.0, < 4.0.5; first patched version not specified)
  - Microsoft.Data.SqlClient:2.1.4 (declared at packages.config:14; vulnerable range >= 5.0.0, < 5.1.3; first patched version not specified)

Recommended fix: Upgrade affected dependencies to a patched version or later.

## CWE Findings (Code-Level Vulnerabilities)

### CWE-682: Incorrect Calculation
- **Category:** Code Quality
- **Severity:** potential
- **Story Points:** 5
- **Files:** PaginatedList.cs

PaginatedList.Create multiplies request-derived pageIndex (passed from StudentsController.Index, line 58) by pageSize using 32-bit integer arithmetic at line 27. Extreme or negative page values can overflow and produce an unintended Skip offset, so the computed paging result is not reliable.

### CWE-732: Incorrect Permission Assignment for Critical Resource
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** Services/NotificationService.cs

NotificationService creates a message queue and grants MessageQueueAccessRights.FullControl to the Everyone principal at line 23. This allows unintended local actors to read, write, or alter notification messages.

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** Controllers/CoursesController.cs

In the POST Edit action, TeachingMaterialImagePath is included in the model binding allowlist, and the value is passed to Server.MapPath at line 172 before the resulting path is deleted at line 175. A crafted relative pathname can therefore escape the intended upload directory.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** Controllers/CoursesController.cs

The POST Edit action binds TeachingMaterialImagePath from the request and maps it at line 172 without verifying that the resolved path remains under the uploads directory; relative traversal segments can reach other filesystem paths before deletion.

### CWE-36: Absolute Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** Controllers/CoursesController.cs

The POST Edit action binds TeachingMaterialImagePath from the request and passes it to Server.MapPath at line 172 without constraining the resolved path. An absolute or rooted path value can cause the subsequent File.Delete call at line 175 to target a path outside the upload directory.

### CWE-434: Unrestricted Upload of File with Dangerous Type
- **Category:** File & Path Security
- **Severity:** mandatory
- **Story Points:** 8
- **Files:** Controllers/CoursesController.cs

The Create and Edit actions validate uploads only by the client-supplied filename extension at lines 55-60 and 138-143, then save the raw content beneath the web-root Uploads directory at lines 76-87 and 159-180. No content signature or actual file type validation prevents arbitrary content from being stored with an allowed image suffix.

### CWE-502: Deserialization of Untrusted Data
- **Category:** Injection Attacks
- **Severity:** mandatory
- **Story Points:** 13
- **Files:** Services/NotificationService.cs

ReceiveNotification reads the message body from the message queue at line 75 and deserializes it to Notification at line 76 without validating the resulting fields. The service creates the queue with FullControl permissions for Everyone at lines 20-23, allowing untrusted local writers to supply message bodies.
