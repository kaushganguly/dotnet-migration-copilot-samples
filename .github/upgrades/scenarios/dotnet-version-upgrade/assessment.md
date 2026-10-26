# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

Detailed findings live alongside this file in `assessment/`. This page is the index: read it first, then open only the documents you need.

## Table of Contents

- [Executive Summary](#executive-summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Detailed Reports](#detailed-reports)
  - [Projects Relationship Graph](assessment/project-graph.md)
  - [Aggregate NuGet packages details](assessment/nuget/aggregate-packages.md)
  - [Most Frequent API Issues (complete list)](assessment/api-issues/most-frequent-api-issues.md)
  - [Project Details](#project-details)

## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 1 | All require upgrade |
| Total NuGet Packages | 0 | All compatible |
| Total Code Files | 0 |  |
| Total Code Files with Incidents | 0 |  |
| Total Lines of Code | 0 |  |
| Total Number of Issues | 0 |  |
| Proposed Target Framework | net10.0 |  |
| Estimated LOC to modify | 0+ | at least 0.0% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Binding Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 0 | 0.0% |
| ⚠️ Incompatible | 0 | 0.0% |
| 🔄 Upgrade Recommended | 0 | 0.0% |
| ***Total NuGet Packages*** | ***0*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |

The table above is the top 10. See [the complete list](assessment/api-issues/most-frequent-api-issues.md) for every affected API.

## Detailed Reports

- [Projects Relationship Graph](assessment/project-graph.md)
- [Aggregate NuGet packages details](assessment/nuget/aggregate-packages.md)
- [Most Frequent API Issues (complete list)](assessment/api-issues/most-frequent-api-issues.md)

### Project Details




---

## Environment Limitation Note

> Automated analysis via `generate_dotnet_upgrade_assessment` could not fully process
> `ContosoUniversity.csproj` in this Linux CI environment because the legacy web
> application project imports `Microsoft.WebApplication.targets`, a Visual-Studio-only
> MSBuild target not present outside Windows + Visual Studio. The tool returned 0
> projects/issues as a result (see `assessment.json`).
>
> To compensate, the agent performed a manual, file-by-file analysis of the project
> (csproj, packages.config, Web.config, Global.asax.cs, App_Start/*, Controllers/*,
> Views/**, Data/*, Services/*, Models/*) to build the migration plan below. Key
> findings from that manual review:
>
> - **Already on EF Core 3.1** for data access (`Microsoft.EntityFrameworkCore*` 3.1.32) — only
>   the EF Core package versions need bumping to a .NET-10-compatible release; no EDMX/EF6 migration needed.
> - **ASP.NET MVC 5** (`System.Web.Mvc` 5.2.9) is used for the web layer — full ASP.NET Core MVC
>   migration required (Program.cs host, DI, routing, controllers, Razor views, static assets).
> - Razor views use classic `HtmlHelper` methods (`Html.ActionLink`, `Html.BeginForm`, `Html.EditorFor`,
>   `Html.DisplayFor`, etc.) which have direct ASP.NET Core equivalents — low migration risk.
> - `@Scripts.Render` / `@Styles.Render` (System.Web.Optimization bundling) must be replaced with
>   direct `<script>`/`<link>` tags; static assets (`Content/`, `Scripts/`) must move to `wwwroot/`.
> - `NotificationsController.GetNotifications()` returns JSON consumed by `Scripts/notifications.js`,
>   which expects **PascalCase** property names (`notification.CreatedAt`, `.Message`, etc.) — ASP.NET
>   Core's default `System.Text.Json` camelCases property names, so `PropertyNamingPolicy = null` must
>   be configured to preserve the existing wire shape and avoid breaking the client-side JS (internal,
>   same-app endpoint — not an externally consumed contract).
> - `Services/NotificationService.cs` uses `System.Messaging` (MSMQ) — migrate to the `MSMQ.Messaging`
>   NuGet package (same API surface, different namespace) per the `migrating-to-msmq-messaging` skill.
> - No test projects exist in the repository — `passUnitTests` success criterion is vacuously satisfied.
> - No authentication/authorization, no OWIN, no custom DI container, no Web API/OData controllers,
>   no HttpModules/HttpHandlers beyond the default `Global.asax` MVC bootstrap — this substantially
>   reduces migration risk/scope versus a typical ASP.NET Framework app.
