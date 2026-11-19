# Task 02 — Progress Details

## Summary
Migrated ContosoUniversity from ASP.NET MVC 5 (System.Web, .NET Framework 4.8) to ASP.NET Core MVC on net10.0. Build succeeds with **0 warnings, 0 errors**, and `dotnet list package --vulnerable --include-transitive` reports **no vulnerable packages**.

## Hosting model
- **Added** `Program.cs`: minimal hosting model. Registers `SchoolContext` (`AddDbContext`, SQL Server provider), `NotificationService` (scoped), binds `MessageQueueOptions` from configuration section `MessageQueue`, configures `AddControllersWithViews().AddJsonOptions(...)` with `PropertyNamingPolicy = null` / `DictionaryKeyPolicy = null` to preserve the MVC 5 `JsonResult` PascalCase-preserving wire contract consumed by `Scripts/notifications.js` (recorded gate evidence in task.md). Seeds the DB via `DbInitializer.Initialize` in a DI scope (replaces `Global.asax.cs Application_Start`). Pipeline: `UseExceptionHandler`/`UseStatusCodePagesWithReExecute` (replaces `FilterConfig`'s `HandleErrorAttribute`), `UseStaticFiles`, `UseRouting`, single default `MapControllerRoute` (replaces `RouteConfig`).
- **Removed**: `Global.asax`, `Global.asax.cs`, `App_Start/RouteConfig.cs`, `App_Start/FilterConfig.cs`, `App_Start/BundleConfig.cs`, `Data/SchoolContextFactory.cs` (superseded by DI registration), `Properties/AssemblyInfo.cs` (conflicted with SDK auto-generated assembly attributes — CS0579).

## Configuration
- **Added** `appsettings.json` / `appsettings.Development.json`: `ConnectionStrings:DefaultConnection`, `MessageQueue:QueuePath`, default `Logging`/`AllowedHosts`.
- **Added** `Services/MessageQueueOptions.cs` (simple options POCO bound via `IOptions<T>`).
- **Removed** `Web.config` and `Views/Web.config` entirely (all `<system.web>`/`<system.webServer>` entries are Framework-only; `<connectionStrings>` and the `NotificationQueuePath` appSetting were migrated to appsettings.json first).

## Controllers (all 6)
- Namespace swap `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc` (and removed `System.Net` usings).
- `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`; `HttpNotFound()` → `NotFound()`.
- `[Bind(Include = "a,b,c")]` → `[Bind("a", "b", "c")]` (Core's `BindAttribute.Include` is a read-only `string[]` populated via a `params string[]` constructor, not a settable comma-joined string — CS0617 otherwise).
- **BaseController**: converted from the old field-initializer/manual-`new` pattern to constructor injection of `SchoolContext` and `NotificationService`; removed the manual `Dispose(bool)` override that called `db.Dispose()` (the DI container now owns the scoped `SchoolContext` lifetime).
- **CoursesController**: added `IWebHostEnvironment` constructor dependency; `HttpPostedFileBase` → `IFormFile`; `Server.MapPath` → `Path.Combine(_env.WebRootPath, ...)`; synchronous `SaveAs` → `async Task<ActionResult>` actions using `await teachingMaterialImage.CopyToAsync(stream)`.
- **InstructorsController**: `TryUpdateModel(model, prefix, string[] includeProperties)` → `await TryUpdateModelAsync(model, prefix, propertyExpressions...)` (Core has no string-array-include overload); `Edit(POST)` action became `async Task<ActionResult>`; `selectedCourses` parameters made `string[]?` (nullable, matches real-world optional post data); null-guarded `instructorToUpdate.OfficeAssignment` before dereferencing `.Location`.
- **NotificationsController**: dropped `JsonRequestBehavior.AllowGet` arguments (no Core equivalent/need).
- **DepartmentsController / StudentsController**: namespace + status-code-result swaps only; removed manual `Dispose(bool)` overrides.

## Services
- **NotificationService.cs**: `using System.Messaging;` → `using MSMQ.Messaging;` (drop-in replacement package, same class/method names); constructor now takes `IOptions<MessageQueueOptions>` instead of reading `ConfigurationManager.AppSettings` directly. `ReceiveNotification()` return type changed to `Notification?` (nullable, matches "queue empty" return path). Added `MSMQ.Messaging` 1.0.4 package reference. (MSMQ remains a Windows-only runtime dependency — this affects only runtime behavior on non-Windows hosts, not compilation; only build validation is possible in this Linux CI environment.)

## Views
- **Added** `Views/_ViewImports.cshtml` (`@using` project namespaces + `Microsoft.AspNetCore.Mvc.Rendering` + `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`).
- **Verified via throwaway probe build** that classic MVC 5 HtmlHelper extensions (`Html.ActionLink`, `Html.BeginForm`, `Html.DropDownList`, `Html.TextBox`, `Html.ValidationSummary`, `Html.LabelFor`/`EditorFor`/`DisplayFor`/`ValidationMessageFor`, `Html.AntiForgeryToken`) all compile unchanged under ASP.NET Core 10's `Microsoft.AspNetCore.Mvc.Rendering` — **no TagHelper conversion was required** for any of the 23 views.
- **Views/Shared/Error.cshtml**: removed `@model System.Web.Mvc.HandleErrorInfo` and the `HttpContext.Current.IsDebuggingEnabled` conditional block (no Core equivalent / view model class no longer exists); now a static error view.
- **Bundling removed**: `@Scripts.Render(...)`/`@Styles.Render(...)` (no Core equivalent) replaced with direct `<script>`/`<link>` tags pointing at the same relative paths, across `_Layout.cshtml` and the 8 Create/Edit views that referenced `~/bundles/jqueryval`.
- **Nullable-reference fixes**: `model.Administrator!.FullName` / `model.OfficeAssignment!.Location` null-forgiving operator added in `Html.*For` expression-tree lambdas (Departments Index/Details/Delete, Instructors Create/Edit) — these are expression-tree-only accesses used purely for member-name resolution and are never invoked against a real instance, so `!` is safe and matches the nullable model changes below.

## Models (nullable-reference enablement)
`<Nullable>enable</Nullable>` was added to the csproj in Task 01; this task's full compile surfaced the deferred CS8618 warnings across all `Models/*.cs` POCOs. Fixed by:
- Required scalar strings (`Title`, `Name`, `LastName`, `FirstMidName`, `EntityType`, `EntityId`, `Operation`, `Message`, etc.) → initialized to `string.Empty`.
- Optional scalars (`TeachingMaterialImagePath`, `RequestId`, `CreatedBy`, `Location`) → declared `string?`.
- One-to-zero-or-one navigation properties (`Department.Administrator`, `Instructor.OfficeAssignment`) → declared nullable (`Instructor?` / `OfficeAssignment?`), matching real-world optionality.
- Required navigation/collection properties (`Course.Department`, `CourseAssignment.Instructor/Course`, `Enrollment.Course/Student`, `*.Courses`/`*Enrollments`/`*CourseAssignments` collections) → `= null!;` (standard EF Core pattern; EF materializes these before use).
- `Department.RowVersion` (byte[], `[Timestamp]`) → `= Array.Empty<byte>();`.
- `InstructorIndexData`'s three `IEnumerable<T>` properties → `= Enumerable.Empty<T>();`.

## Static assets
- Moved `Content/`, `Scripts/`, `Uploads/` into `wwwroot/` via `git mv` (preserves history) — required by ASP.NET Core's `UseStaticFiles()` convention (serves only from `wwwroot/`). No favicon.ico existed in the original project. View/layout references use the same relative paths (`~/Content/...`, `~/Scripts/...`), now resolved against `wwwroot/`.

## Security
- Pinned `System.Drawing.Common` to `11.0.0` (direct `PackageReference` override) to eliminate a transitive `4.7.0` reference flagged by NU1904 (GHSA-rxg9-xrhp-64gj, critical). Confirmed via `dotnet list package --vulnerable --include-transitive`: **no vulnerable packages** remain.

## Validation
- `dotnet build ContosoUniversity.csproj --no-incremental`: **Build succeeded, 0 Warning(s), 0 Error(s).**
- `dotnet list package --vulnerable --include-transitive`: no vulnerable packages.
- No test projects exist in the solution (`ContosoUniversity.sln` references only `ContosoUniversity.csproj`) — `generateNewUnitTests=false` per task success criteria, so `passUnitTests=true` is trivially satisfied.
- Runtime smoke-testing (actually running the app against a live SQL Server / MSMQ) was out of scope for this Linux CI environment — SQL Server and MSMQ are Windows-only runtime dependencies untouched by this migration beyond the documented package/namespace swaps.
