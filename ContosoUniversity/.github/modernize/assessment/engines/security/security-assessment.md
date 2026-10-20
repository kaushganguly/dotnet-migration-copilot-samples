# Security Assessment Report

**Generated:** 2026-10-20T12:06:51.5419753Z

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
| mandatory | 1 |
| optional | 4 |
| potential | 3 |

## CVE Findings (Dependency Vulnerabilities)

### CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** packages.config:14

[CVE-2024-0056](https://github.com/advisories/GHSA-98g6-xh36-x2p7): Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass

Severity: HIGH

Affected dependency:
  - Microsoft.Data.SqlClient 2.1.4 (packages.config:14; affected range: < 2.1.7)

Recommended fix:
  - Upgrade Microsoft.Data.SqlClient to 2.1.7 or later.

## CWE Findings (Code-Level Vulnerabilities)

### CWE-662: Improper Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** Services/NotificationService.cs:20-23, Controllers/BaseController.cs:12

NotificationService instances are constructed for controller requests, and the constructor separately checks MessageQueue.Exists before creating the shared queue. Concurrent first requests can both observe the queue as absent and race to create the same resource; this check-then-create sequence has no synchronization.

### CWE-820: Missing Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** Services/NotificationService.cs:20-23, Controllers/BaseController.cs:12

Each request creates a NotificationService instance that may initialize the same MSMQ queue. The Exists-then-Create operation is not synchronized, so simultaneous requests can race during shared queue initialization.

### CWE-732: Incorrect Permission Assignment for Critical Resource
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** Services/NotificationService.cs:20-23

When creating its private notification queue, NotificationService explicitly grants the Everyone principal MessageQueueAccessRights.FullControl. This allows any actor represented by Everyone to read, write, and administer a resource intended for application notifications.

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172

The Edit action binds TeachingMaterialImagePath from the request and, when an image is uploaded, passes that value directly to Server.MapPath before deleting the resolved file. The path is not validated to remain under the TeachingMaterials directory, allowing path elements such as ../ to resolve outside the intended directory.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172

The Edit action binds the request-controlled TeachingMaterialImagePath and passes it to Server.MapPath, then File.Exists and File.Delete, without rejecting relative traversal sequences such as ../.

### CWE-79: Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')
- **Category:** Injection Attacks
- **Severity:** optional
- **Story Points:** 8
- **Files:** Scripts/notifications.js:80-84, Services/NotificationService.cs:39-49, Controllers/CoursesController.cs:102

Course titles are accepted from requests and passed as entityDisplayName when notifications are created. NotificationService.GenerateMessage embeds that value in Message, which is returned by NotificationsController.GetNotifications and inserted into notificationEl.innerHTML without HTML encoding in notifications.js. A title containing markup can execute in the browser of users viewing notifications.

### CWE-99: Improper Control of Resource Identifiers ('Resource Injection')
- **Category:** Injection Attacks
- **Severity:** potential
- **Story Points:** 3
- **Files:** Controllers/CoursesController.cs:130, Controllers/CoursesController.cs:172

The Edit action binds TeachingMaterialImagePath from the request and uses the value as a filesystem resource identifier in Server.MapPath, File.Exists, and File.Delete without verifying it refers to an image beneath the intended upload directory.
