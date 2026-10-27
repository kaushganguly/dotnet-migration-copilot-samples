# 001.04-views-configuration-migration: Migrate Razor views and Web.config settings

## Objective
Port all `.cshtml` views to ASP.NET Core MVC and retire System.Web.Optimization bundling.

## Steps
1. Add `Views/_ViewImports.cshtml` with `@using ContosoUniversity`, `@using ContosoUniversity.Models`, `@using ContosoUniversity.Models.SchoolViewModels`, `@using Microsoft.AspNetCore.Mvc.Rendering`, `@using Microsoft.AspNetCore.Mvc.ViewFeatures` (for `Html.BeginForm`/`ActionLink`/etc. extension method resolution).
2. `Views/Shared/_Layout.cshtml`: replace `@Styles.Render("~/Content/css")` / `@Scripts.Render(...)` with direct `<link rel="stylesheet" href="~/Content/site.css" />` plus `<link href="~/Content/notifications.css">`, and `<script src="~/Scripts/jquery-3.4.1.min.js"></script>`, `<script src="~/Scripts/bootstrap.min.js"></script>`.
3. `Views/Shared/Error.cshtml`: replace `@model System.Web.Mvc.HandleErrorInfo` + `HttpContext.Current.IsDebuggingEnabled` with `@model ContosoUniversity.Models.ErrorViewModel` (standard ASP.NET Core error view pattern showing `Model.RequestId` when `Model.ShowRequestId`).
4. Remove `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }` blocks (6 views: Courses/Create+Edit, Departments/Create+Edit, Instructors/Create+Edit, Students/Create+Edit) — replace with direct script tags for `jquery.validate.min.js` + `jquery.validate.unobtrusive.min.js`, or drop the section entirely if unobtrusive validation scripts are already referenced in `_Layout.cshtml`.
5. All other `Html.*` helper usages (`BeginForm`, `EditorFor`, `DisplayFor`, `DisplayNameFor`, `ValidationMessageFor`, `ValidationSummary`, `ActionLink`, `DropDownList`, `HiddenFor`, `TextBox`, `Raw`) are source-compatible with ASP.NET Core MVC and need no changes beyond the `_ViewImports.cshtml` usings from step 1.
6. Delete `Views/Web.config` (obsolete under ASP.NET Core).
7. Web.config → appsettings.json: already covered by task 001.02 (`ConnectionStrings`, no custom appSettings needed beyond what NotificationService now ignores).
