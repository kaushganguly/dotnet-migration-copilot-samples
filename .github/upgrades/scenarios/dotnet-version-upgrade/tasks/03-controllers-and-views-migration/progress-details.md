# 03 — Controllers and views migration

**Status:** Completed

## Changes (all controllers)
- `using System.Web.Mvc;` → `using Microsoft.AspNetCore.Mvc;`; removed `using System.Web;` /
  `using System.Net;`.
- `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`; `HttpNotFound()` → `NotFound()`.
- Removed manual `Dispose(bool disposing)` overrides from `BaseController`, `DepartmentsController`,
  `InstructorsController`, `StudentsController` — `SchoolContext` is now DI-scoped and disposed by
  the container.
- `BaseController` rewritten for constructor injection: `(SchoolContext context, INotificationService
  notificationService)`; all derived controllers (`Home`, `Students`, `Departments`, `Instructors`,
  `Notifications`, `Courses`) updated with matching DI constructors.
- `[Bind(Include = "A,B,C")]` (MVC5 named-argument syntax, not supported by
  `Microsoft.AspNetCore.Mvc.BindAttribute`, which only exposes a `params string[]` constructor) →
  `[Bind("A", "B", "C")]` in `CoursesController`, `DepartmentsController`, `InstructorsController`,
  `StudentsController`.
- `CoursesController`: added `IWebHostEnvironment` constructor dependency and a `MapUploadPath`
  helper (replaces `Server.MapPath`); `HttpPostedFileBase` → `IFormFile`; `.ContentLength` → `.Length`;
  `.SaveAs(path)` → manual `FileStream` + `CopyTo`.
- `InstructorsController.Edit(int?, string[])`: MVC5's `TryUpdateModel(model, prefix, string[]
  includeProperties)` overload has no ASP.NET Core equivalent — converted the action to
  `async Task<IActionResult>` using `await TryUpdateModelAsync<Instructor>(instructorToUpdate, "",
  i => i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment)`.
- `NotificationsController`: removed `JsonRequestBehavior.AllowGet` (ASP.NET Core's `Json()` always
  allows GET).
- `HomeController.Unauthorized()`: added `new` modifier (hides `ControllerBase.Unauthorized()`,
  was a compiler warning, not an error, under the old MVC5 base class).
- Views: removed all `@Styles.Render`/`@Scripts.Render` bundling calls (see task 02 for
  `_Layout.cshtml`/Error view detail). No Razor views used Tag Helpers (`asp-*` attributes) or
  other System.Web-specific helpers — `Html.ActionLink`, `Html.LabelFor`, `Html.EditorFor`,
  `Html.DisplayNameFor`, `Html.ValidationMessageFor`, `Html.HiddenFor` all work unchanged via
  `Microsoft.AspNetCore.Mvc.ViewFeatures` / `Microsoft.AspNetCore.Mvc.Rendering`.

## Wire-compatibility decision
Per the `migrating-webapi-odata` Wire-Contract Gate (recorded in `scenario-instructions.md` under
"Wire Compatibility"): **not applicable**. The only JSON endpoint
(`NotificationsController.GetNotifications`/`MarkAsRead`) is consumed solely by the in-repo
`wwwroot/Scripts/notifications.js`, migrated in the same pass — no OData, no external consumers.
PascalCase JSON field names are preserved via the `Program.cs` JSON naming-policy override (see
task 02) specifically so `notifications.js` keeps working unmodified.

## Verified
`dotnet build` / `dotnet publish` succeeded with 0 errors/0 warnings (publish forces Razor
view compile-time checking). Manual smoke test against a live SQL Server confirmed all page
routes return HTTP 200 and `[ValidateAntiForgeryToken]` correctly rejects an unauthenticated
POST (expected security behavior, unchanged from the original app).
