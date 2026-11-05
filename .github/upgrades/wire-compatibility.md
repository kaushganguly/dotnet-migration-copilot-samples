# Wire Compatibility Gate — ContosoUniversity

## Scope
ContosoUniversity.csproj — ASP.NET MVC 5 (System.Web) → ASP.NET Core MVC, in-place, net10.0.

## Applicability Check

- No `Microsoft.AspNet.WebApi.OData` / `Microsoft.AspNet.OData` / OData package referenced in `packages.config`.
- No `ODataEntityTypeSerializer` or OData controllers/EDM models in the codebase.
- Only one JSON-returning action: `NotificationsController.GetNotifications()` (`JsonResult`).
  Its sole consumer is `Scripts/notifications.js` (`fetch('/Notifications/GetNotifications', ...)`),
  which is part of this same application's front-end, shipped and migrated together with the
  Razor views that reference it. No external/third-party consumer, no documented contract,
  no contract tests, no API documentation referencing this endpoint.
- All other controllers (`CoursesController`, `DepartmentsController`, `HomeController`,
  `InstructorsController`, `StudentsController`) return `View`/`ActionResult` (Razor HTML), not
  JSON/XML/OData payloads.

## Decision

**NOT APPLICABLE** — no OData usage, no Atom formatters, no custom OData serializers, no pinned
non-OData shape consumed externally, and no repository/user hint requiring wire-contract
preservation. The only JSON endpoint is consumed exclusively by this application's own
client-side script, migrated in the same task.

Proceeding with in-place ASP.NET Core migration for the full project, including
`NotificationsController` (ported as a plain ASP.NET Core MVC JSON action returning the same
shape via `Json(...)`).
