namespace ContosoUniversity.Services
{
    /// <summary>
    /// Configuration for the MSMQ notification queue, bound from the
    /// "MessageQueue" section of appsettings.json (replaces the
    /// Web.config appSettings["NotificationQueuePath"] + ConfigurationManager usage).
    /// </summary>
    public class MessageQueueOptions
    {
        public string QueuePath { get; set; }
    }
}
