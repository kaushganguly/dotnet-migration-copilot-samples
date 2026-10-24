# Task 03: NuGet dependency updates

## Objective
Replace `packages.config` with `PackageReference` and update all NuGet dependencies to
versions compatible with `net10.0`.

## Final package set (`ContosoUniversity.csproj`)
| Package | Version | Notes |
|---|---|---|
| Microsoft.Data.SqlClient | 7.1.1 | Was 2.1.4 (had a known security advisory per assessment) |
| Microsoft.EntityFrameworkCore | 10.0.13 | Replaces EF6 (`EntityFramework` package) |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.13 | SQL Server provider for EF Core |
| Microsoft.EntityFrameworkCore.Tools | 10.0.13 | Design-time tools (migrations, scaffolding) |
| MSMQ.Messaging | 1.0.4 | Replaces built-in `System.Messaging` (Windows-only, not available on .NET Core/5+); queue connection made lazy so DI construction doesn't require MSMQ to be present |
| Newtonsoft.Json | 13.0.4 | Was pulled transitively/implicitly in MVC5; now explicit, latest stable |
| System.Drawing.Common | 10.0.13 | Pinned — transitive dependency of other packages pulled an older vulnerable version (NU1902/03/04 advisories) |
| System.DirectoryServices | 10.0.13 | Pinned for the same reason |
| System.Security.Cryptography.Pkcs | 10.0.13 | Pinned for the same reason |

Packages removed entirely (no longer applicable under ASP.NET Core):
Microsoft.AspNet.Mvc, Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages,
Microsoft.AspNet.Web.Optimization, Microsoft.Web.Infrastructure, EntityFramework (EF6),
Antlr, WebGrease, Modernizr (package; the JS file itself is kept as a static asset),
Microsoft.CodeDom.Providers.DotNetCompilerPlatform, bootstrap/jQuery NuGet packages
(static assets now served directly from `wwwroot/`), and all `Microsoft.AspNet.*`
/`Microsoft.Owin*` family packages (not used — no OWIN/auth in this app, confirmed during
assessment: 0 `HttpContext.Current` usages, no FormsAuth/Membership/OWIN markers found).

## Verification
- `dotnet restore` — succeeds.
- `dotnet list package --vulnerable --include-transitive` — "no vulnerable packages".
- `dotnet list package --deprecated` — "no deprecated packages".
