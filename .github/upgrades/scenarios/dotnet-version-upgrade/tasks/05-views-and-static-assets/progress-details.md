# Progress — 05-views-and-static-assets

## What changed
- `Views/Shared/Error.cshtml`: `@model System.Web.Mvc.HandleErrorInfo` +
  `HttpContext.Current.IsDebuggingEnabled` → `@model ErrorViewModel` (the pre-existing, previously
  unused `Models/ErrorViewModel.cs`) showing `RequestId` when present — the standard ASP.NET Core
  template error-view pattern. Dev-mode exception details are now handled by
  `app.UseDeveloperExceptionPage()` (wired in `Program.cs`, task 02), not this view.
- Added `Views/_ViewImports.cshtml` (`@using ContosoUniversity`, `@using ContosoUniversity.Models`)
  — fixes `Views/Students/Index.cshtml`'s `@model PaginatedList<...>` (type lives in the project
  root namespace, which Razor views don't see implicitly the way `.cs` files do).
- Moved `Content/`, `Scripts/`, `Uploads/` to `wwwroot/Content`, `wwwroot/Scripts`,
  `wwwroot/Uploads` via `git mv` (preserves history) — the only location
  `app.UseStaticFiles()` serves by default, and the location `CoursesController`'s
  `IWebHostEnvironment.WebRootPath`-based upload path (task 04) assumes.
- Deleted `Views/Web.config` (MVC5 Razor-view-compilation config, no purpose under Core).
- `Views/Shared/_Layout.cshtml`: replaced `@Styles.Render("~/Content/css")` /
  `@Scripts.Render("~/bundles/modernizr|jquery|bootstrap")` with direct `<link>`/`<script>` tags.
  **Deviation (documented)**: the old `~/Content/css` bundle referenced `Content/bootstrap.css`,
  which does not exist anywhere in the repository (pre-existing broken/dead reference, confirmed
  via `ls`), and the only local bootstrap JS asset is v3.4.1 (jQuery-dependent), while the navbar
  markup already uses Bootstrap 5 classes (`navbar-expand-lg`, `collapse navbar-collapse`) — a
  pre-existing version mismatch unrelated to this .NET upgrade. Replaced with Bootstrap 5.3.3 CDN
  `<link>`/`<script>` (css + bundle.min.js, includes Popper) so the existing v5 markup actually
  renders/functions. Local `jquery-3.4.1.js`, `modernizr-2.6.2.js`, `notifications.js` kept as
  direct script tags against `wwwroot`. `respond.js` (IE8 polyfill) moved with the rest of
  `Scripts/` but intentionally left unreferenced (dead weight for supported browser targets).
- Replaced `@Scripts.Render("~/bundles/jqueryval")` with direct
  `<script src="~/Scripts/jquery.validate.js">` + `<script src="~/Scripts/jquery.validate.unobtrusive.js">`
  tags in all 8 Create/Edit views that had it (Departments, Courses, Students, Instructors —
  task.md originally estimated 6, corrected to 8 after grep confirmed both Create and Edit for
  every one of the 4 entities use it).

## Validation
- `grep -rn "Scripts.Render\|Styles.Render\|HttpContext.Current" Views/` → zero matches.
- `dotnet build` — **zero errors and zero warnings originating from `Views/`**. Remaining 22
  build errors are entirely confined to `App_Start/*.cs` + `Global.asax.cs` (task 06 scope).

## Files Modified
- Views/Shared/Error.cshtml (rewritten)
- Views/_ViewImports.cshtml (added)
- Views/Shared/_Layout.cshtml (rewritten)
- Views/Departments/Edit.cshtml, Views/Departments/Create.cshtml
- Views/Courses/Edit.cshtml, Views/Courses/Create.cshtml
- Views/Students/Edit.cshtml, Views/Students/Create.cshtml
- Views/Instructors/Edit.cshtml, Views/Instructors/Create.cshtml
- Views/Web.config (deleted)
- Content/ → wwwroot/Content/ (git mv)
- Scripts/ → wwwroot/Scripts/ (git mv)
- Uploads/ → wwwroot/Uploads/ (git mv)
