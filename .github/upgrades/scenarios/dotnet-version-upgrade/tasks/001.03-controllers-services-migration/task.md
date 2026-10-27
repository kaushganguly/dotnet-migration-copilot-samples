# 001.03-controllers-services-migration: Migrate controllers, BaseController, NotificationService, EF Core factory

## Objective
Port all C# application code from System.Web.Mvc/System.Web APIs to Microsoft.AspNetCore.Mvc equivalents, and replace MSMQ-based NotificationService with an in-process implementation.

## Steps
1. `BaseController`: inject `SchoolContext` + `NotificationService` via constructor (DI) instead of `SchoolContextFactory.Create()` / `new NotificationService()`. Remove manual `db`/`notificationService` disposal (DI-managed scoped/singleton lifetimes); keep `SendEntityNotification` helper.
2. All controllers (`HomeController`, `StudentsController`, `CoursesController`, `DepartmentsController`, `InstructorsController`, `NotificationsController`): change `using System.Web.Mvc;` → `using Microsoft.AspNetCore.Mvc;`; `HttpNotFound()` → `NotFound()`; `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`; `[Bind(Include = "a,b")]` → `[Bind("a,b")]`; `JsonRequestBehavior.AllowGet` argument removed from `Json(...)` calls.
3. `CoursesController` file upload: replace `HttpPostedFileBase` with `IFormFile`, `.ContentLength` → `.Length`, `Server.MapPath(...)` → inject `IWebHostEnvironment` and use `Path.Combine(_env.WebRootPath, "Uploads", "TeachingMaterials")`, `.SaveAs(path)` → `using var stream = System.IO.File.Create(path); await file.CopyToAsync(stream);` (make the two actions `async Task<IActionResult>`).
4. `InstructorsController.Edit(int?, string[])` POST: replace `TryUpdateModel(instructorToUpdate, "", new[]{...})` with `await TryUpdateModelAsync(instructorToUpdate, "", i => i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment)` (make action async).
5. `NotificationService` (Services/NotificationService.cs): remove `System.Messaging`/`ConfigurationManager` dependency; back the queue with `System.Collections.Concurrent.ConcurrentQueue<Notification>` keeping the same public method signatures (`SendNotification` x2 overloads, `ReceiveNotification`, `MarkAsRead`, `Dispose`); register as DI singleton so the queue is shared across requests.
6. `Data/SchoolContextFactory.cs`: replace the `ConfigurationManager`-based runtime factory with `SchoolContextDesignTimeFactory : IDesignTimeDbContextFactory<SchoolContext>` reading `appsettings.json` (for `dotnet ef` design-time tooling only); the running app gets `SchoolContext` through DI registered in `Program.cs`.
7. `Data/DbInitializer.cs`: no change needed (pure EF Core API, called from `Program.cs` now instead of `Global.asax.cs`).
