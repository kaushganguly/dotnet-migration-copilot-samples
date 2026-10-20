using System.Linq;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ContosoUniversity.Data;
using ContosoUniversity.Services;
using ContosoUniversity.Models;
using ContosoUniversity.Models.SchoolViewModels;

namespace ContosoUniversity.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(SchoolContext db, NotificationService notificationService)
            : base(db, notificationService)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            IQueryable<EnrollmentDateGroup> data = 
                from student in db.Students
                group student by student.EnrollmentDate into dateGroup
                select new EnrollmentDateGroup()
                {
                    EnrollmentDate = dateGroup.Key,
                    StudentCount = dateGroup.Count()
                };
            return View(data.ToList());
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        public new IActionResult Unauthorized()
        {
            ViewBag.Message = "You don't have permission to access this resource.";
            return View();
        }
    }
}
