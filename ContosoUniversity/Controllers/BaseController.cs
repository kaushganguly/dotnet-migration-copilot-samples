using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly SchoolContext db;
        protected readonly NotificationService notificationService;

        protected BaseController(SchoolContext context, NotificationService notifications)
        {
            db = context;
            notificationService = notifications;
        }

        protected void SendEntityNotification(string entityType, string entityId, EntityOperation operation)
        {
            SendEntityNotification(entityType, entityId, null, operation);
        }

        protected void SendEntityNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
        {
            var userName = User.Identity?.Name ?? "System";
            notificationService.SendNotification(entityType, entityId, entityDisplayName, operation, userName);
        }
    }
}
