# Progress — 001.04-views-configuration-migration

## Changes
- Added `Views/_ViewImports.cshtml`: `@using` for `ContosoUniversity`, `ContosoUniversity.Models`,
  `ContosoUniversity.Models.SchoolViewModels`, `Microsoft.AspNetCore.Mvc.Rendering`,
  `Microsoft.AspNetCore.Mvc.ViewFeatures` so `Html.*` extension methods (`ActionLink`,
  `DropDownList`, `EditorFor`, etc.) and `SelectList` resolve without per-view usings.
- `Views/Shared/_Layout.cshtml`: replaced `@Styles.Render("~/Content/css")` /
  `@Scripts.Render(...)` calls with direct `<link>`/`<script>` tags referencing the actual
  files present under `wwwroot/` (`Content/Site.css`, `Content/notifications.css`,
  `Scripts/modernizr-2.6.2.js`, `Scripts/jquery-3.4.1.js`, `Scripts/bootstrap.js`,
  `Scripts/respond.js`, `Scripts/notifications.js`). Note: the original `~/Content/css`
  bundle also referenced `~/Content/bootstrap.css`, but that file does not actually exist in
  the repository (only `Site.css`/`notifications.css` do) — this was already a dangling
  reference before the migration, so it was dropped rather than fabricated.
- `Views/Shared/Error.cshtml`: changed `@model System.Web.Mvc.HandleErrorInfo` +
  `HttpContext.Current.IsDebuggingEnabled` to `@model ContosoUniversity.Models.ErrorViewModel`
  with the standard ASP.NET Core `Model.ShowRequestId`/`Model.RequestId` pattern.
- Removed the `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }` block in 8 views
  (`Courses/Create.cshtml`, `Courses/Edit.cshtml`, `Departments/Create.cshtml`,
  `Departments/Edit.cshtml`, `Instructors/Create.cshtml`, `Instructors/Edit.cshtml`,
  `Students/Create.cshtml`, `Students/Edit.cshtml`), replacing it with direct
  `<script src="~/Scripts/jquery.validate.min.js">` + `jquery.validate.unobtrusive.min.js`
  tags (both files exist under `wwwroot/Scripts`).
- Verified (via grep) no other views reference `System.Web`, `HttpContext.Current`,
  `JsonRequestBehavior`, `@Scripts`/`@Styles`, or `HttpPostedFileBase` — all other `Html.*`
  helper usages across Courses/Departments/Instructors/Students/Home/Notifications views are
  source-compatible with `Microsoft.AspNetCore.Mvc` as-is.
- Confirmed `Courses/Create.cshtml` and `Courses/Edit.cshtml` file-upload forms
  (`enctype="multipart/form-data"`, `<input type="file" name="teachingMaterialImage">`)
  already match the `IFormFile teachingMaterialImage` action parameter name from task 001.03
  — no markup changes needed there.
- `Views/Web.config` was already removed in task 001.02 (confirmed, not re-deleted here).

## Validation
- `grep -rln "System.Web|HttpContext.Current|JsonRequestBehavior|@Scripts|@Styles|HttpPostedFileBase" Views/`
  → no matches remaining.
- Full build/runtime validation deferred to task 001.05.
