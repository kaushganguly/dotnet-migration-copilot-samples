# Progress: 01-sdk-style-and-packages

## Changes Made

- Deleted legacy `ContosoUniversity.csproj` (ToolsVersion 15.0, `TargetFrameworkVersion=v4.8`,
  46 explicit `<Reference>` entries, web-application MSBuild imports).
- Created new SDK-style `ContosoUniversity.csproj`:
  - `Sdk="Microsoft.NET.Sdk.Web"`, `TargetFramework=net10.0`
  - `PackageReference` set: `Microsoft.EntityFrameworkCore` 10.0.14, `Microsoft.EntityFrameworkCore.SqlServer`
    10.0.14, `Microsoft.EntityFrameworkCore.Tools` 10.0.14, `Microsoft.Data.SqlClient` 7.1.1,
    `Newtonsoft.Json` 13.0.4, `MSMQ.Messaging` 1.0.4 (replaces `System.Messaging`)
  - Pinned transitive packages `System.DirectoryServices`, `System.Drawing.Common`,
    `System.Security.Cryptography.Pkcs` to `10.0.14` to resolve NU1902/NU1903/NU1904
    security-advisory warnings surfaced by `dotnet restore` (older transitive versions pulled
    in by `MSMQ.Messaging`/`Microsoft.Data.SqlClient`)
  - Excluded `Uploads/**` from the default `Content` glob and re-included as `None` with
    `CopyToOutputDirectory=PreserveNewest` (user-uploaded file storage, not deployable content)
  - Removed all ASP.NET MVC 5 / WebPages / Razor / Optimization / WebGrease / Antlr /
    Microsoft.CodeDom.Providers.DotNetCompilerPlatform / Microsoft.Web.Infrastructure /
    Microsoft.jQuery.Unobtrusive.Validation / NETStandard.Library / Microsoft.Bcl.* /
    Microsoft.Identity.Client / jQuery / jQuery.Validation / bootstrap / Modernizr package
    references — superseded by the ASP.NET Core shared framework (`Microsoft.NET.Sdk.Web`)
    or retained as static vendored files under `Scripts/`/`Content/` (moved to `wwwroot/` in task 02)
- Deleted `packages.config`.
- Deleted `Properties/AssemblyInfo.cs` (superseded by SDK-style auto-generated assembly info).

## Validation

- `dotnet restore` succeeds with **0 warnings, 0 errors** (previously reported NU1902/NU1903/NU1904
  security-advisory warnings, resolved by pinning transitive package versions — see above).
- Full `dotnet build` is expected to fail at this point (`System.Web.*` symbols still referenced by
  `Global.asax.cs`, `App_Start/*.cs`, `Controllers/*.cs`, `Views/*.cshtml`) — this is expected per
  the project-file-first migration phasing and is resolved in task `02-aspnetcore-migration`.

## Files Modified

- `ContosoUniversity/ContosoUniversity.csproj` (replaced)
- `ContosoUniversity/packages.config` (deleted)
- `ContosoUniversity/Properties/AssemblyInfo.cs` (deleted)
