namespace ContosoUniversity.Services
{
    public class MessageQueueOptions
    {
        public string NotificationQueuePath { get; set; } = @".\Private$\ContosoUniversityNotifications";
    }
}
