# Progress — 03-data-and-services

## What changed
- Rewrote `Services/NotificationService.cs`: replaced `System.Messaging.MessageQueue` +
  `XmlMessageFormatter` + `Newtonsoft.Json` round-trip with an in-process
  `ConcurrentQueue<Notification>` (`SendNotification` → `Enqueue`, `ReceiveNotification` →
  `TryDequeue`). All public method signatures preserved exactly
  (`SendNotification` both overloads, `ReceiveNotification()`, `MarkAsRead(int)` — still a no-op
  as in the original, `Dispose()`). Constructor now takes `IConfiguration` (DI) instead of reading
  `ConfigurationManager.AppSettings` directly; `Notifications:QueuePath` value is preserved for
  configuration-surface parity even though the in-process queue doesn't use it.
- Deleted `Data/SchoolContextFactory.cs` (its only caller, `BaseController`, is updated in task 04
  to use constructor-injected `SchoolContext` instead, now that `Program.cs` registers
  `AddDbContext<SchoolContext>`).
- Deleted `Services/LoggingService.cs` — confirmed empty (0 lines) and zero references anywhere
  in the codebase before deleting.

## Deviation (documented, rationale-backed)
Did not use the `MSMQ.Messaging` NuGet package suggested by the generic MSMQ-migration pattern:
it is a drop-in namespace-compatible package but still requires a live Windows MSMQ service at
runtime, which does not exist on Linux or in typical modern cross-platform .NET 10 deployment
targets. An in-process `ConcurrentQueue` is behaviorally equivalent for this single-process app
(same-process producer/consumer, no cross-machine queue was ever actually required by the
existing code) and keeps the app genuinely cross-platform, which is a stated goal of the upgrade.

## Validation
- `dotnet build` — zero errors originating from `Data/` or `Services/` files (confirmed via
  targeted grep on build output). Remaining build errors are entirely in not-yet-migrated
  `Controllers/`/`Models`/`Views` files, in scope for tasks 04/05.

## Files Modified
- Services/NotificationService.cs (rewritten)
- Data/SchoolContextFactory.cs (deleted)
- Services/LoggingService.cs (deleted)
