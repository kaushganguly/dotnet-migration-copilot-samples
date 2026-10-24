# Progress: 02-aspnetcore-migration

- Added `Program.cs` (minimal hosting), `appsettings.json`, `appsettings.Development.json`,
  `Properties/launchSettings.json`.
- Fixed JSON casing: `AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null)`
  so `/Notifications/GetNotifications` and `/Notifications/MarkAsRead` keep PascalCase JSON
  property names expected by `wwwroot/Scripts/notifications.js`.
- Ported all 7 controllers, `Data/SchoolContext.cs`, `Services/NotificationService.cs` (+ new
  `Services/MessageQueueOptions.cs`), and all Models/ViewModels with nullable annotations.
- Ported all Views, removed bundling calls, added `_ViewImports.cshtml`.
- Moved `Content/`, `Scripts/`, `Uploads/` to `wwwroot/`.
- Removed `Global.asax(.cs)`, `App_Start/*`, `Views/Web.config`, `Web.config`,
  `Data/SchoolContextFactory.cs`, `Services/LoggingService.cs` (dead file), `Properties/AssemblyInfo.cs`.
- `dotnet build` succeeds, 0 warnings / 0 errors. `dotnet run` constructs the full DI
  container/host; fails only at the LocalDB connection step, which is an environment
  limitation (no LocalDB on Linux), not a code defect.
