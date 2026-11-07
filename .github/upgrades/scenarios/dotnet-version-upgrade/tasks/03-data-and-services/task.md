# 03-data-and-services

## Objective
Port `Data/` layer to be DI-friendly (remove `ConfigurationManager`-based factory) and replace
`Services/NotificationService.cs`'s MSMQ backend with an in-process queue.

## Research Findings
- `Data/SchoolContext.cs`: already EF Core 3.1-style `DbContext`, constructor takes
  `DbContextOptions<SchoolContext>` — **no changes needed**, it is DI-ready as-is; only consumer
  (`SchoolContextFactory`) needs removal since Program.cs (task 02) registers it via
  `AddDbContext` directly.
- `Data/SchoolContextFactory.cs`: static factory reading `ConfigurationManager.ConnectionStrings`
  — **DELETE**. Only caller was `BaseController`'s constructor (fixed in task 04 to use
  constructor injection instead).
- `Data/DbInitializer.cs`: static `Initialize(SchoolContext)` — already framework-agnostic,
  **no changes needed** (already wired from `Program.cs` in task 02).
- `Services/NotificationService.cs` (confirmed by full read): uses `System.Messaging.MessageQueue`
  (create-if-missing, `XmlMessageFormatter`), `ConfigurationManager.AppSettings["NotificationQueuePath"]`,
  `Newtonsoft.Json` for message body serialization. Public surface used by callers:
  - `SendNotification(entityType, entityId, operation, userName=null)` (overload)
  - `SendNotification(entityType, entityId, entityDisplayName, operation, userName=null)`
  - `ReceiveNotification()` → returns next notification or `null` if none available (used by
    `NotificationsController.GetNotifications` in a `while` loop, up to 10 at a time, as a
    destructive dequeue-and-return poll)
  - `MarkAsRead(int notificationId)` → **currently a no-op** (comment confirms: "In a real
    implementation, you might want to store..." — nothing to port, keep as a no-op)
  - `Dispose()`
  - **Decision (documented deviation from generic `migrating-to-msmq-messaging` skill)**:
    `MSMQ.Messaging` NuGet (the direct namespace-compatible replacement package) still requires
    a real Windows MSMQ service at runtime and would not function on Linux/this environment or
    in any cross-platform .NET 10 deployment — defeating a core purpose of the upgrade. Replacing
    the backing store with `System.Collections.Concurrent.ConcurrentQueue<Notification>`
    (registered as a DI singleton so the queue instance is shared across requests, mirroring the
    previous shared Windows MSMQ queue's semantics) preserves exact method signatures so
    `BaseController`/`NotificationsController` (task 04) need no call-site changes beyond
    constructor injection.
  - `NotificationQueuePath` config value is no longer functionally meaningful (no real queue path
    to open) but is kept in `appsettings.json`'s `Notifications:QueuePath` and read into the
    service via constructor injection of `IConfiguration`/`IOptions`-style, purely so the setting
    isn't silently orphaned and so a future real-queue backend swap has a documented settings slot.
- `Services/LoggingService.cs`: confirmed **empty file** (0 lines), confirmed **zero references**
  anywhere in the codebase via `grep -rn "LoggingService"` — dead stub, **DELETE**.

## Steps
1. Rewrite `NotificationService.cs`: remove `System.Messaging`/`ConfigurationManager` usings,
   add `System.Collections.Concurrent`, back the queue with `ConcurrentQueue<Notification>`,
   accept `IConfiguration` via constructor (reads `Notifications:QueuePath`, stored but unused
   at runtime — documented above), keep all public method signatures identical.
   `ReceiveNotification()` → `_queue.TryDequeue(out var n) ? n : null`.
   `SendNotification` → `_queue.Enqueue(notification)` (no serialization needed in-process —
   `Newtonsoft.Json` round-trip is removed since it's no longer crossing a process boundary;
   this is a safe simplification, not a behavior change, since `ReceiveNotification` always
   immediately deserialized the exact same object back).
2. Delete `Data/SchoolContextFactory.cs`.
3. Delete `Services/LoggingService.cs` (empty, unreferenced).
4. `dotnet build` — expect remaining errors only from controllers/models/views not yet migrated
   (tasks 04/05); confirm no NEW errors originate from `Data/`/`Services/`.

## Files Modified
- Services/NotificationService.cs (rewritten)
- Data/SchoolContextFactory.cs (deleted)
- Services/LoggingService.cs (deleted)
