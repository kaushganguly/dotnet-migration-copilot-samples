# Progress — 001.03-controllers-services-migration

## Changes
- `BaseController`: constructor now takes `SchoolContext` + `NotificationService` via DI
  (replaces `SchoolContextFactory.Create()` / `new NotificationService()`); removed the
  manual `Dispose(bool)` override (DI manages scoped/singleton lifetimes).
- `HomeController`, `StudentsController`, `DepartmentsController`, `InstructorsController`,
  `NotificationsController`, `CoursesController`: added DI constructors chaining to
  `base(context, notificationService)`; `using System.Web.Mvc;` → `using Microsoft.AspNetCore.Mvc;`
  (+ `Microsoft.AspNetCore.Mvc.Rendering` where `SelectList` is used); `HttpNotFound()` →
  `NotFound()`; `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`;
  `[Bind(Include = "...")]` → `[Bind("...")]`; removed `JsonRequestBehavior.AllowGet` args;
  removed now-redundant `Dispose` overrides.
- `CoursesController`: added `IWebHostEnvironment` constructor dependency; `HttpPostedFileBase`
  → `IFormFile` (`.ContentLength` → `.Length`); `Server.MapPath(...)` → `Path.Combine(_env.WebRootPath, ...)`;
  `.SaveAs(path)` → `FileStream` + `await file.CopyToAsync(stream)`; `Create`/`Edit` POST actions
  made `async Task<ActionResult>`.
- `InstructorsController.Edit` POST: made `async Task<ActionResult>`; replaced
  `TryUpdateModel(instructorToUpdate, "", new[]{"LastName","FirstMidName","HireDate","OfficeAssignment"})`
  with `await TryUpdateModelAsync(instructorToUpdate, "", i => i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment)`.
- `Services/NotificationService.cs`: removed `System.Messaging`/`ConfigurationManager`
  dependency; backed by `ConcurrentQueue<Notification>`, same public method signatures
  (`SendNotification` x2, `ReceiveNotification`, `MarkAsRead`, `Dispose`); registered as a
  DI singleton in `Program.cs` (done in task 001.02). Documented behavioral trade-off:
  notifications no longer persist across app restarts / aren't shared cross-process.
- `Data/SchoolContextFactory.cs`: replaced the `ConfigurationManager`-based runtime factory
  with `IDesignTimeDbContextFactory<SchoolContext>` reading `appsettings.json`, used only by
  `dotnet ef` design-time tooling. Runtime `SchoolContext` resolution happens via DI
  (`Program.cs`, task 001.02).
- `Data/SchoolContext.cs`, `Data/DbInitializer.cs`, `PaginatedList.cs`, `Models/*`: verified —
  pure EF Core / POCO code, no changes needed (confirmed via grep for `System.Web`,
  `ConfigurationManager`, `System.Messaging` — no matches outside the explanatory comment
  left in `NotificationService.cs`).

## Validation
- `grep -rn "System.Web\|ConfigurationManager\|System.Messaging" Data/ Models/ PaginatedList.cs Services/`
  → no remaining legacy API usages.
- Full build deferred to task 001.05 (Views still reference `System.Web.Mvc`-only helpers
  like `@Scripts.Render` until task 001.04 lands).
