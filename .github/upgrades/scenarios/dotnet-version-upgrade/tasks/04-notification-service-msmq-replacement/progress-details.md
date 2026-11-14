# 04 — Notification service (MSMQ) replacement

**Status:** Completed — accepted behavioral change, documented risk

## Why
`System.Messaging` (MSMQ) is Windows-only and has no implementation on .NET Core / .NET 5+ or on
Linux. It could not be preserved as-is.

## Changes
- Added `Services/INotificationService.cs` (new interface: `SendNotification` x2 overloads,
  `ReceiveNotification`, `MarkAsRead`).
- Rewrote `Services/NotificationService.cs` to implement the interface via an in-process
  `ConcurrentQueue<Notification>`, registered as a DI singleton (`AddSingleton<INotificationService,
  NotificationService>()` in `Program.cs`) so the queue is shared across requests the same way the
  original MSMQ queue was shared across processes.
- Deleted `Data/SchoolContextFactory.cs` (its only consumer, `BaseController`'s manual
  `SchoolContextFactory.Create()` call, was removed in favor of constructor-injected
  `SchoolContext`).

## Accepted risk (recorded in plan.md Risk Areas)
Semantics change from a **persistent, cross-process** message queue (MSMQ survives process
restarts) to an **in-process, non-persistent** queue (`ConcurrentQueue`) — notifications queued
before an app restart/crash are lost. This is an unavoidable consequence of MSMQ having no
cross-platform equivalent; no .NET-Core-compatible drop-in replacement preserves the same
persistence guarantees without introducing a new external dependency (e.g., a real message broker),
which was out of scope for this upgrade. The rationale is also documented inline as a doc comment
in `NotificationService.cs`.
