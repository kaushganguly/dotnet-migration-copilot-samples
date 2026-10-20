using System;
using Microsoft.AspNetCore.Mvc;
using ContosoUniversity.Data;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers
{
    public class NotificationsController : BaseController
    {
        public NotificationsController(SchoolContext db, NotificationService notificationService)
            : base(db, notificationService)
        {
        }

        // GET: api/notifications - Get pending notifications for admin
        [HttpGet]
        public JsonResult GetNotifications()
        {
            try
            {
                var notifications = notificationService.GetUnreadNotifications();
                return Json(new
                {
                    success = true,
                    notifications,
                    count = notifications.Count
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving notifications: {ex.Message}");
                return Json(new { success = false, message = "Error retrieving notifications" });
            }
        }

        // POST: api/notifications/mark-read
        [HttpPost]
        public JsonResult MarkAsRead(int id)
        {
            try
            {
                notificationService.MarkAsRead(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
                return Json(new { success = false, message = "Error updating notification" });
            }
        }

        // GET: Notifications/Index - Admin notification dashboard
        public IActionResult Index()
        {
            return View();
        }
    }
}
