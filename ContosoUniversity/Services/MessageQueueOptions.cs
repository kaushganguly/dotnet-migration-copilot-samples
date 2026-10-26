namespace ContosoUniversity.Services
{
    /// <summary>
    /// Strongly-typed configuration bound from the "NotificationQueue" section of appsettings.json
    /// (replaces the ConfigurationManager.AppSettings["NotificationQueuePath"] lookup used under
    /// ASP.NET Framework).
    /// </summary>
    public class MessageQueueOptions
    {
        public string QueuePath { get; set; } = @".\Private$\ContosoUniversityNotifications";
    }
}
