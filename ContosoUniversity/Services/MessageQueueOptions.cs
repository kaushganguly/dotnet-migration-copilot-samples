namespace ContosoUniversity.Services
{
    /// <summary>
    /// Options bound from the "MessageQueue" section of appsettings.json.
    /// Replaces the legacy <c>NotificationQueuePath</c> appSetting read via
    /// <c>System.Configuration.ConfigurationManager</c>, which is not available on .NET Core/.NET 10.
    /// </summary>
    public class MessageQueueOptions
    {
        public string QueuePath { get; set; } = @".\Private$\ContosoUniversityNotifications";
    }
}
