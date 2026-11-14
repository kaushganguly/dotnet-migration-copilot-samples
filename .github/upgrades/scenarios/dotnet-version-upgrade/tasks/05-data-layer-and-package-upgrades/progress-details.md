# 05 — Data layer and package upgrades

**Status:** Completed

## Changes
- `Data/SchoolContext.cs`, `Data/DbInitializer.cs`, `Models/*`, `PaginatedList.cs` required **no
  structural changes** — the app already used EF Core (3.1.32), not EF6, so TPH discriminator
  mapping, composite keys, and seed logic carried over unchanged; only the package version needed
  bumping.
- Package versions resolved via `get_supported_package_version` for `net10.0` and pinned in
  `ContosoUniversity.csproj`:
  - `Microsoft.EntityFrameworkCore` 3.1.32 → **10.0.15**
  - `Microsoft.EntityFrameworkCore.SqlServer` 3.1.32 → **10.0.15**
  - `Microsoft.Data.SqlClient` → **7.1.2**
- Connection string migrated from `Web.config` to `appsettings.json`
  (`ConnectionStrings:DefaultConnection`), with `TrustServerCertificate=True` added (required by
  modern `Microsoft.Data.SqlClient` defaults).
- Verified end-to-end against a live SQL Server container: `EnsureCreated()` created the full
  schema (`Notification`, `Person`/TPH `Student`/`Instructor`, `Department`, `OfficeAssignment`,
  `Course`, `CourseAssignment`, `Enrollment`) and seed data inserted without error.

## Security
`dotnet list package --vulnerable --include-transitive` reports **no vulnerable packages** —
satisfies `securityComplianceCheck=true`.
