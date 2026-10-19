# Admin notifications

Controllers publish entity create, update, and delete notifications through `NotificationService`. The service uses a bounded in-process channel, and `NotificationsController` exposes the messages to the polling UI.

Notifications are best-effort, shared only by this application process, and are not persisted. They are dropped when the process restarts; the bounded channel drops the oldest message when full. This removes the prior dependency on Windows-only MSMQ and does not require queue setup.

To add notifications to a controller, inject the shared `NotificationService` through `BaseController` and call `SendEntityNotification` after a successful database save.

Run the app with a configured SQL Server connection using the .NET 10 SDK. No MSMQ feature, Windows-only service, or `Web.config` setting is required.
