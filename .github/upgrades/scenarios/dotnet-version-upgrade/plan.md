# Upgrade Plan: ContosoUniversity to .NET 10.0

## Selected Strategy

**All-At-Once** - Single project in solution, no dependency graph to manage.
**Rationale**: 1 project (ContosoUniversity.csproj), currently net48, non-SDK-style ASP.NET MVC Web Application Project. Single-project .NET Framework solutions always use All-at-Once per the framework-migration planning rules, even though Bottom-Up is the general default for multi-project Framework solutions.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project in solution, no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | Small web project (6 controllers, ~2,000 LOC), low complexity; matches requested full MVC to ASP.NET Core rewrite |
| Unsupported Packages | Resolve Inline | Only 2 packages lack compatible versions (Antlr -> Antlr4; Microsoft.AspNet.Web.Optimization -> remove/replace with static tags) |
| Windows Native APIs | No Compatibility Pack | Single isolated usage (MSMQ/System.Messaging in NotificationService); no supported cross-platform MSMQ package; build/test environment is Linux |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small project, in-place approach, zero HttpContext.Current usages found |
| Assembly Binding Redirects | Remove Binding Redirects | All 18 redirects are standard VS-generated patterns for minor version bumps; SDK-style auto-manages this |
| Nullable Reference Types | Enable Nullable Reference Types | Small codebase (~2,000 LOC), single project, low complexity assessment |

## Project List

- ContosoUniversity.csproj (Web Application Project, ASP.NET MVC 5 / System.Web, currently net48, non-SDK-style, packages.config) - single project in scope, in-place migration to ASP.NET Core MVC on net10.0.

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity.csproj to SDK-style project format

Convert the legacy non-SDK-style ContosoUniversity.csproj (old ToolsVersion="15.0" Web Application Project format, packages.config-based package management) to the modern SDK-style project format, while remaining on the current net48 target framework. This is a structural change only - it does not touch the target framework or attempt any ASP.NET Core migration yet.

Scope: the single project ContosoUniversity.csproj. The assessment shows 45 NuGet packages currently referenced via packages.config; these must become PackageReference items as part of the conversion. Several GAC/framework assembly references (System.Web, System.Web.Mvc, System.Drawing, System.Messaging, System.EnterpriseServices, etc. - see assessment Project.0001/Project.0002) are present and must be preserved or translated correctly for the SDK-style format while still on net48. Assembly binding redirects (18 entries in Web.config, all standard VS-generated patterns) should be removed as part of this cleanup per the confirmed "Remove Binding Redirects" option, since SDK-style projects auto-manage binding redirects.

Research starting points: inventory the full ItemGroup reference list and packages.config contents, check Web.config for assemblyBinding entries to remove, and confirm the convert_project_to_sdk_style tool output builds cleanly on net48 before moving to task 02.

**Done when**: ContosoUniversity.csproj is SDK-style, uses PackageReference (no packages.config), still targets net48, assembly binding redirects are removed from Web.config, and the project builds successfully on net48 with no new warnings.

### 02-aspnet-core-migration: Migrate ContosoUniversity from ASP.NET MVC (net48) to ASP.NET Core MVC (net10.0)

Retarget ContosoUniversity.csproj to net10.0 and migrate the application in-place from ASP.NET MVC 5 / System.Web to ASP.NET Core MVC - hosting model (Global.asax.cs to Program.cs/minimal hosting), routing (RouteConfig to endpoint routing), controllers (System.Web.Mvc.Controller to Microsoft.AspNetCore.Mvc.Controller), views/Razor (.cshtml updates for Core Razor engine and tag helpers), and configuration (Web.config appSettings/connectionStrings to appsettings.json + IConfiguration). This is a Direct Migration (no Microsoft.AspNetCore.SystemWebAdapters shim layer), per the confirmed System.Web Adapters = Direct Migration option, since the project is small and has zero HttpContext.Current usages.

Scope: the single project ContosoUniversity.csproj, all 6 controllers (BaseController, CoursesController, DepartmentsController, HomeController, InstructorsController, NotificationsController, StudentsController), 28 views, Global.asax.cs, App_Start/* (RouteConfig, BundleConfig, FilterConfig), Web.config, and Services/NotificationService.cs.

Known risks and required package/API work from the assessment:
- System.Web.Optimization bundling/minification (Feature.0001) is not supported in ASP.NET Core - replace BundleConfig/@Scripts.Render/@Styles.Render usage with direct script/link tags to static content files, served via ASP.NET Core static files middleware. Remove the incompatible Microsoft.AspNet.Web.Optimization and WebGrease packages.
- Global.asax.cs application initialization (Feature.1000) must be converted to ASP.NET Core startup/hosting code and Global.asax/Global.asax.cs removed once equivalent behavior is established in Program.cs.
- Antlr 3.4.1.9004 is incompatible with net10.0 - replace with Antlr4 4.6.6 per assessment recommendation (check actual grammar/parser usage before assuming a drop-in replacement; if Antlr is unused dead weight, prefer removal).
- Microsoft.AspNet.Mvc, Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages, Microsoft.Web.Infrastructure, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, NETStandard.Library, and other packages flagged "functionality is included with framework reference" should be removed - their functionality is native to ASP.NET Core / the modern SDK.
- Microsoft.Data.SqlClient 2.1.4 has a known security vulnerability - upgrade to 7.1.2.
- Microsoft.Identity.Client 4.21.1 is deprecated - upgrade to the latest supported version compatible with net10.0 (check actual usage first; this project may not need it if unused after migration).
- Microsoft.EntityFrameworkCore* packages (currently 3.1.32) should be upgraded to 10.0.15, and Data/ DbContext/configuration code reviewed for EF Core 3.1 to 10 breaking changes.
- Services/NotificationService.cs uses System.Messaging.MessageQueue (MSMQ) - a Windows-only API with no supported .NET 10 / cross-platform package (confirmed Windows Native APIs = No Compatibility Pack). Replace with a cross-platform-compatible abstraction (e.g., an INotificationQueue interface with an in-process/file-based or channel-based implementation) that preserves the existing SendNotification method signatures and JSON message shape, so NotificationsController and any tests depending on it continue to work unchanged in behavior.
- Enable <Nullable>enable</Nullable> on the project per the confirmed Nullable Reference Types option, and address the resulting warnings as part of this task (do not suppress).
- Web.config appSettings (webpages:Version, webpages:Enabled, ClientValidationEnabled, UnobtrusiveJavaScriptEnabled, NotificationQueuePath) and connectionStrings (DefaultConnection) must move to appsettings.json / ASP.NET Core configuration providers; the webpages:*/ClientValidationEnabled/UnobtrusiveJavaScriptEnabled keys are MVC5-specific and likely obsolete under ASP.NET Core's client validation model - confirm during execution and drop if unneeded.
- #skill:migrating-webapi-odata does not apply - this project has no OData/WebAPI endpoints based on the assessment and controller inventory (standard MVC controllers only); skip loading that skill unless execution uncovers otherwise.

This task's Done-when criteria includes a full solution build and the full existing test suite, since this is the last implementation task in an All-at-Once, single-project plan. Record any deferred post-upgrade recommendations (e.g., further MSMQ feature parity hardening, further Antlr grammar modernization) in this task's Done-when/notes rather than creating a separate validation task.

**Done when**: ContosoUniversity.csproj targets net10.0, builds with 0 errors and 0 warnings, all controllers/views/configuration run on ASP.NET Core MVC with no remaining System.Web references, Global.asax/Global.asax.cs are removed (functionality ported to Program.cs), the notification feature works via a cross-platform-compatible mechanism (no MSMQ/System.Messaging dependency), nullable reference types are enabled with warnings addressed, and the full test suite passes.
