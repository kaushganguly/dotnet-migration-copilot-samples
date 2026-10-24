namespace ContosoUniversity.Services
{
    /// <summary>
    /// Options bound from the "MessageQueue" section of appsettings.json.
    /// Replaces the classic Web.config "NotificationQueuePath" appSetting, read in the
    /// legacy app via <c>ConfigurationManager.AppSettings</c>.
    /// </summary>
    public class MessageQueueOptions
    {
        public string QueuePath { get; set; } = @".\Private$\ContosoUniversityNotifications";
    }
}
