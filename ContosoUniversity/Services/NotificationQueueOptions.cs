namespace ContosoUniversity.Services
{
    /// <summary>
    /// Options bound from the "NotificationQueue" section in appsettings.json.
    /// Replaces the old Web.config "NotificationQueuePath" appSetting.
    /// </summary>
    public class NotificationQueueOptions
    {
        public string QueuePath { get; set; } = @".\Private$\ContosoUniversityNotifications";
    }
}
