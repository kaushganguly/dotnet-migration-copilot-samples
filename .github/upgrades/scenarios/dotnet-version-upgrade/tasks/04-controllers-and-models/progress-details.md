# Progress — 04-controllers-and-models

## What changed
- All 7 controllers (`BaseController`, `HomeController`, `DepartmentsController`,
  `StudentsController`, `InstructorsController`, `CoursesController`, `NotificationsController`)
  rewritten: `using System.Web.Mvc;` → `using Microsoft.AspNetCore.Mvc;` (+ `.Rendering` for
  `SelectList` where used).
- `[Bind(Include = "A,B,C")]` (MVC5 string-property syntax) → `[Bind("A", "B", "C")]` (Core's
  `BindAttribute(params string[])` constructor syntax) — applied to all 14 occurrences across
  Departments/Students/Instructors/Courses controllers.
- `HttpNotFound()` → `NotFound()`; `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` →
  `BadRequest()` — `using System.Net;` dropped everywhere it was only used for this.
- `NotificationsController`: dropped the `JsonRequestBehavior.AllowGet` argument from both
  `Json(...)` calls (no Core equivalent/need — GET+JSON unrestricted by default); confirmed no
  wire-format/casing change since `Program.cs` (task 02) already sets
  `JsonSerializerOptions.PropertyNamingPolicy = null` to preserve PascalCase for `notifications.js`.
- `CoursesController` file upload (`Create`/`Edit` POST): `HttpPostedFileBase` → `IFormFile`
  (`Microsoft.AspNetCore.Http`), `.ContentLength` → `.Length`, `.SaveAs(path)` →
  `using (var stream = new FileStream(path, FileMode.Create)) { file.CopyTo(stream); }`,
  `Server.MapPath(...)` → constructor-injected `IWebHostEnvironment.WebRootPath` +
  `Path.Combine`. Old-file-deletion logic in `Edit`/`DeleteConfirmed` similarly rewritten to
  resolve paths via `WebRootPath` instead of `Server.MapPath`.
- `InstructorsController.Edit(int?, string[])`: synchronous `TryUpdateModel(model, prefix,
  string[] includeProperties)` → `async Task<ActionResult> Edit(...)` using
  `await TryUpdateModelAsync(model, prefix, propertyExpression1, propertyExpression2, ...)` —
  the closest behavior-preserving Core equivalent (restricts bound properties to the same 4
  fields: `LastName`, `FirstMidName`, `HireDate`, `OfficeAssignment`).
- DI constructor injection: `BaseController` now takes `(SchoolContext, NotificationService)` via
  constructor (replacing `SchoolContextFactory.Create()` + `new NotificationService()` field
  initializers); every derived controller adds an explicit constructor forwarding to `base(...)`
  (`CoursesController` additionally injects `IWebHostEnvironment`).
- Removed manual `Dispose(bool)` overrides from `BaseController`/`CoursesController`/
  `DepartmentsController`/`InstructorsController`/`StudentsController` — `db` (scoped) and
  `notificationService` (singleton) are now container-owned; the ASP.NET Core DI container
  disposes `IDisposable` scoped/singleton services automatically, so manual disposal here would
  be redundant.
- Fixed one build **warning** (CS0114, not a pre-existing error — introduced by the namespace
  swap since `Microsoft.AspNetCore.Mvc.ControllerBase` declares its own `Unauthorized()` helper
  method, which `HomeController`'s custom `Unauthorized()` action now hides): added the `new`
  keyword to `HomeController.Unauthorized()` since it is intentionally a different, pre-existing
  custom action (returns a view, not an HTTP 401 result) — confirmed via repo-wide grep that
  nothing references this action by name, so the rename-vs-`new`-keyword tradeoff was decided in
  favor of `new` to keep the action's route/name unchanged.
- Confirmed via full read: `Models/*.cs` (`Course`, `CourseAssignment`, `Department`,
  `Enrollment`, `ErrorViewModel`, `Instructor`, `Notification`, `OfficeAssignment`, `Person`,
  `Student`, `SchoolViewModels/*`) use only `System.ComponentModel.DataAnnotations`/BCL types —
  **no changes required**.

## Validation
- `dotnet build` — **zero errors and zero warnings originating from `Controllers/` or `Models/`**.
  Remaining 30 build errors are entirely confined to `App_Start/*.cs` + `Global.asax.cs` (task 06
  scope — deleted there) and `Views/Shared/Error.cshtml` + `Views/Students/Index.cshtml` (task 05
  scope — `@Scripts.Render`/`PaginatedList` Razor-view fixes).

## Files Modified
- Controllers/BaseController.cs
- Controllers/HomeController.cs
- Controllers/DepartmentsController.cs
- Controllers/StudentsController.cs
- Controllers/InstructorsController.cs
- Controllers/CoursesController.cs
- Controllers/NotificationsController.cs
