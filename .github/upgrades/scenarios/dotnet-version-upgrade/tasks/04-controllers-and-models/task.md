# 04-controllers-and-models

## Objective
Migrate all 7 controller files from `System.Web.Mvc` to `Microsoft.AspNetCore.Mvc`, fix the
`CoursesController` file-upload code path, and switch `BaseController`/derived controllers from
field-initializer construction to ASP.NET Core DI constructor injection (now that
`SchoolContextFactory` is gone and `Program.cs` registers `SchoolContext`/`NotificationService`
in the container). Confirmed `Models/*.cs` (incl. `SchoolViewModels/*`) use only
`System.ComponentModel.DataAnnotations`/BCL types — **no changes needed** there.

## Research Findings (full read of all 7 controller files + build error triage)
- Build error triage (`dotnet build` before this task) categorizes cleanly into:
  `System.Web.Mvc`-namespace types (`ActionResult`, `Controller`, `[HttpPost]`,
  `[ValidateAntiForgeryToken]`, `[Bind]`, `[ActionName]`, `JsonResult`, `HttpPostedFileBase`) and
  `System.Web.Routing`/`System.Web.Optimization`/`HttpApplication` (App_Start/Global.asax files,
  out of scope here — task 06 deletes them).
- **Namespace swap** (all 7 files): `using System.Web.Mvc;` → `using Microsoft.AspNetCore.Mvc;`.
  Attribute names (`HttpPost`, `ValidateAntiForgeryToken`, `ActionName`, `Bind`) and `ActionResult`
  keep identical names in the new namespace — only the `using` changes.
- **`[Bind(Include = "A,B,C")]` → `[Bind("A", "B", "C")]`**: ASP.NET Core's `BindAttribute` has a
  constructor `BindAttribute(params string[] include)` and a read-only `Include` array property
  (no settable `Include=` named-argument, unlike MVC5's string-valued property) — every
  `[Bind(Include = "...")]` usage across Courses/Departments/Instructors/Students controllers
  (14 occurrences) must become comma-separated **individual string arguments**, not a single
  joined string, to avoid relying on undocumented splitting behavior.
- **`HttpNotFound()` → `NotFound()`**, **`new HttpStatusCodeResult(HttpStatusCode.BadRequest)` →
  `BadRequest()`** — both have direct `ControllerBase` helper-method equivalents in Core; the
  `using System.Net;` import (only used for `HttpStatusCode.BadRequest`) is dropped everywhere.
- **`SelectList`**: used in Departments/Courses (`ViewBag.XxxID = new SelectList(...)`) — Core
  equivalent is `Microsoft.AspNetCore.Mvc.Rendering.SelectList`, same constructor shape
  (`IEnumerable`, dataValueField, dataTextField, selectedValue) — add
  `using Microsoft.AspNetCore.Mvc.Rendering;`, no call-site changes needed.
- **`NotificationsController.GetNotifications`/`MarkAsRead`**: drop the `JsonRequestBehavior.AllowGet`
  second argument to `Json(...)` (Core's `Json()` helper has no such concept — GET+JSON is
  unrestricted by default) — this was already classified NOT-APPLICABLE for the
  `migrating-webapi-odata` wire-contract gate (no OData/external contract; see assessment.md).
- **`CoursesController` file upload** (`HttpPostedFileBase teachingMaterialImage` in both
  `Create`/`Edit` POST actions): `HttpPostedFileBase` → `Microsoft.AspNetCore.Http.IFormFile`;
  `.ContentLength` → `.Length`; `.SaveAs(path)` → `using (var stream = System.IO.File.Create(path))
  { teachingMaterialImage.CopyTo(stream); }`; `Server.MapPath("~/Uploads/TeachingMaterials/")` has
  no Core equivalent (`Server`/`HttpContext.Server` don't exist) — replaced with
  constructor-injected `IWebHostEnvironment.WebRootPath` + `Path.Combine(..., "Uploads",
  "TeachingMaterials")`. NOTE: static assets move under `wwwroot/` in task 05, so
  `WebRootPath`-relative paths are correct only once that move happens — both tasks are
  consistent (Uploads ends up at `wwwroot/Uploads/TeachingMaterials/`).
- **`InstructorsController.Edit(int?, string[])` `TryUpdateModel(...)`**: MVC5's synchronous
  `TryUpdateModel(model, prefix, string[] includeProperties)` has no direct sync/string-array
  equivalent in Core — `ControllerBase.TryUpdateModelAsync<TModel>(model, prefix, params
  Expression<Func<TModel,object>>[] includeExpressions)` is the closest 1:1 replacement
  (same semantics: restrict model binding to named properties). Requires making this action
  method `async Task<ActionResult>` and `await`-ing the call; add `using System.Threading.Tasks;`.
- **DI constructor injection** (all 7 controllers, since `SchoolContextFactory`/field-initializer
  `new NotificationService()` are gone — see task 03): `BaseController` gets a
  `protected BaseController(SchoolContext context, NotificationService notificationService)`
  constructor setting `readonly` `db`/`notificationService` fields; every derived controller
  (Courses, Departments, Home, Instructors, Notifications, Students — none currently declare an
  explicit constructor) needs an explicit constructor forwarding to `base(...)` since the base no
  longer has a parameterless constructor. `CoursesController` additionally injects
  `IWebHostEnvironment` for the file-upload path.
- **Remove manual `Dispose(bool)` overrides** (`BaseController`, `CoursesController`,
  `DepartmentsController`, `InstructorsController`, `StudentsController`): `db`
  (`SchoolContext`, scoped) and `notificationService` (singleton) are now container-managed —
  ASP.NET Core's DI container disposes scoped/singleton `IDisposable` services automatically at
  the end of the owning scope/app lifetime; manual disposal here would be redundant/incorrect
  now that the controller doesn't own these instances.

## Steps
1. Rewrite `BaseController.cs` — DI constructor, drop `Dispose` override.
2. Rewrite `HomeController.cs` — namespace + constructor only (simplest file, no other issues).
3. Rewrite `DepartmentsController.cs` — namespace, `Bind` syntax, `HttpNotFound`/`HttpStatusCodeResult`,
   `SelectList` using, constructor, drop `Dispose` override.
4. Rewrite `StudentsController.cs` — same category of fixes as Departments (no SelectList/file
   upload in this one), constructor, drop `Dispose` override (currently a no-op comment stub).
5. Rewrite `InstructorsController.cs` — same fixes + the `TryUpdateModelAsync` async conversion,
   constructor, drop `Dispose` override.
6. Rewrite `CoursesController.cs` — same fixes + `IFormFile`/`IWebHostEnvironment` file-upload
   rewrite, constructor (incl. `IWebHostEnvironment`), drop `Dispose` override.
7. Rewrite `NotificationsController.cs` — namespace, drop `JsonRequestBehavior.AllowGet` args,
   constructor.
8. `dotnet build` — confirm zero errors originate from `Controllers/`; remaining errors should be
   confined to `Views/*.cshtml` (task 05) and dead `Global.asax`/`App_Start` files (task 06).

## Files Modified
- Controllers/BaseController.cs
- Controllers/HomeController.cs
- Controllers/DepartmentsController.cs
- Controllers/StudentsController.cs
- Controllers/InstructorsController.cs
- Controllers/CoursesController.cs
- Controllers/NotificationsController.cs
