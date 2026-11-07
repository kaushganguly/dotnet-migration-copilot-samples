# 05-views-and-static-assets

## Objective
Fix the 2 remaining Razor view build errors, remove `@Scripts.Render`/`@Styles.Render` (no
Core equivalent — `System.Web.Optimization` doesn't exist under `Microsoft.NET.Sdk.Web`), move
static assets (`Content/`, `Scripts/`, `Uploads/`) under `wwwroot/` (the only directory
`app.UseStaticFiles()` serves by default), and add a minimal `_ViewImports.cshtml`.

## Research Findings
- Full `find Views -type f` inventory confirmed: 26 `.cshtml` files + `Views/Web.config` +
  `Views/_ViewStart.cshtml`. `_ViewStart.cshtml`'s `Layout = "~/Views/Shared/_Layout.cshtml"`
  path is unchanged/still valid under Core. `Views/Web.config` (MVC5's view-compilation config)
  has no Core purpose — deleted here (not waiting for task 06, since it lives inside `Views/`
  which this task owns).
- Build-error-confirmed issues (only 2 of 26 views actually error, since most use only
  `Html.*`/`ViewBag` which resolve via the Razor-generated base class regardless of `_ViewImports`):
  - `Views/Shared/Error.cshtml`: `@model System.Web.Mvc.HandleErrorInfo` +
    `HttpContext.Current.IsDebuggingEnabled` (`HttpContext.Current` is a static accessor that
    does not exist in Core) — rewritten to use the already-present (unused until now)
    `ContosoUniversity.Models.ErrorViewModel` (`RequestId`/`ShowRequestId`) — the standard
    ASP.NET Core project-template error view pattern — instead of restoring MVC5-only debug
    exception details (ASP.NET Core's own `UseDeveloperExceptionPage()`, already wired in
    `Program.cs` task 02, is the correct mechanism for that in Development).
  - `Views/Students/Index.cshtml`: `@model PaginatedList<ContosoUniversity.Models.Student>` —
    `PaginatedList<T>` lives in the **root** `ContosoUniversity` namespace (confirmed by reading
    `PaginatedList.cs`), and Razor views (unlike `.cs` files) do NOT automatically see the
    project's root namespace unless it's brought in via `_ViewImports.cshtml` or an explicit
    `@using` — **no `_ViewImports.cshtml` exists yet** (confirmed via directory listing) —
    creating one with `@using ContosoUniversity` resolves this for every view, not just this one.
- `_Layout.cshtml` + 6 Create/Edit views (Departments, Courses, Students, Instructors) use
  `@Scripts.Render("~/bundles/...")`/`@Styles.Render("~/Content/css")` (10 call sites total,
  confirmed via grep) — mapped via `App_Start/BundleConfig.cs` (full content read) to these
  physical files (all confirmed present on disk under `Scripts/`/`Content/` **except**
  `Content/bootstrap.css`, which the old bundle referenced but does **not exist anywhere in the
  repo** — pre-existing dead/broken reference, not introduced by this migration):
  - `~/bundles/jquery` → `Scripts/jquery-3.4.1.js`
  - `~/bundles/jqueryval` → `Scripts/jquery.validate.js` + `Scripts/jquery.validate.unobtrusive.js`
  - `~/bundles/modernizr` → `Scripts/modernizr-2.6.2.js`
  - `~/bundles/bootstrap` → `Scripts/bootstrap.js` (confirmed by content inspection: this is
    actually **Bootstrap 3.4.1** JS, jQuery-dependent) + `Scripts/respond.js`
  - `~/Content/css` → `Content/bootstrap.css` (missing) + `Content/Site.css`
  - **Version mismatch discovered**: `_Layout.cshtml`'s navbar markup
    (`navbar-expand-lg`, `navbar-dark`, `collapse navbar-collapse`) is **Bootstrap 5** markup,
    but the only local bootstrap JS/CSS available is v3.4.1 JS and no CSS at all — this is a
    pre-existing bug unrelated to the .NET upgrade (the nav would already be unstyled/broken
    today). **Decision**: replace the local, version-mismatched, CSS-less Bootstrap asset with
    Bootstrap 5.3.3 via CDN (`<link>`/`<script>` in `_Layout.cshtml`) so the existing v5 markup
    actually renders/functions correctly — a minimal, low-risk fix consistent with "don't widen
    scope" while not leaving a known-broken reference in place. Local `jquery`/`jquery.validate*`
    (unrelated to Bootstrap, needed for client-side form validation) and `modernizr` are kept as
    direct `<script src="~/Scripts/...">` tags against `wwwroot`. `respond.js` (IE8 media-query
    polyfill) is not re-added as a `<script>` tag — dead weight for any currently supported
    browser target; the file itself is still copied to `wwwroot/Scripts/` for parity/no data loss,
    simply unreferenced.
- Static asset directories to move under `wwwroot/` (only directory `UseStaticFiles()` serves by
  convention): `Content/` → `wwwroot/Content/`, `Scripts/` → `wwwroot/Scripts/`, `Uploads/` →
  `wwwroot/Uploads/` (contains `TeachingMaterials/.gitkeep` + one sample image — confirmed
  present, moved as-is; `CoursesController`'s `IWebHostEnvironment.WebRootPath`-based path
  construction from task 04 is only correct once this move happens, so this task closes that
  loop).

## Steps
1. Rewrite `Views/Shared/Error.cshtml` to `@model ContosoUniversity.Models.ErrorViewModel`.
2. Add `@using ContosoUniversity` (+ `ContosoUniversity.Models`) to a new `Views/_ViewImports.cshtml`.
3. `mkdir wwwroot`; `git mv Content wwwroot/Content`; `git mv Scripts wwwroot/Scripts`;
   `git mv Uploads wwwroot/Uploads`.
4. Rewrite `_Layout.cshtml`: replace `@Styles.Render`/`@Scripts.Render` calls with direct
   `<link>`/`<script>` tags (Bootstrap 5 CDN + local jquery/modernizr/notifications.js).
5. Replace the 6 `@Scripts.Render("~/bundles/jqueryval")` call sites (Create/Edit views for
   Departments, Courses, Students, Instructors) with direct
   `<script src="~/Scripts/jquery.validate.js"></script>` +
   `<script src="~/Scripts/jquery.validate.unobtrusive.js"></script>` tags.
6. Delete `Views/Web.config` (MVC5 view-compilation config, no Core purpose).
7. `dotnet build` — confirm 0 errors in `Views/`; remaining errors should be entirely
   `App_Start`/`Global.asax.cs` (task 06 scope).

## Files Modified
- Views/Shared/Error.cshtml (rewritten)
- Views/_ViewImports.cshtml (added)
- Views/Shared/_Layout.cshtml (rewritten)
- Views/{Departments,Courses,Students,Instructors}/{Create,Edit}.cshtml (6 files, Scripts.Render fix)
- Views/Web.config (deleted)
- Content/, Scripts/, Uploads/ → moved to wwwroot/
