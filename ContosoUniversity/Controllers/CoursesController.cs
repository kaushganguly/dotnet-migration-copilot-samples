using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers
{
    public class CoursesController : BaseController
    {
        private readonly IWebHostEnvironment _environment;

        public CoursesController(SchoolContext db, NotificationService notificationService, IWebHostEnvironment environment)
            : base(db, notificationService)
        {
            _environment = environment;
        }

        // GET: Courses
        public IActionResult Index()
        {
            var courses = db.Courses.Include(c => c.Department);
            return View(courses.ToList());
        }

        // GET: Courses/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = db.Courses.Include(c => c.Department).FirstOrDefault(c => c.CourseID == id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name");
            return View(new Course());
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("CourseID,Title,Credits,DepartmentID")] Course course, IFormFile teachingMaterialImage)
        {
            if (ModelState.IsValid)
            {
                // Handle file upload if an image is provided
                if (teachingMaterialImage != null && teachingMaterialImage.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var fileExtension = Path.GetExtension(teachingMaterialImage.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Please upload a valid image file (jpg, jpeg, png, gif, bmp).");
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    // Validate file size (max 5MB)
                    if (teachingMaterialImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "File size must be less than 5MB.");
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    try
                    {
                        // Create uploads directory if it doesn't exist
                        var uploadsPath = Path.Combine(_environment.WebRootPath, "Uploads", "TeachingMaterials");
                        Directory.CreateDirectory(uploadsPath);

                        // Generate unique filename
                        var fileName = $"course_{course.CourseID}_{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, fileName);

                        // Save file
                        using (var stream = new FileStream(filePath, FileMode.CreateNew))
                        {
                            teachingMaterialImage.CopyTo(stream);
                        }
                        course.TeachingMaterialImagePath = $"~/Uploads/TeachingMaterials/{fileName}";
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Error uploading file.");
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }
                }

                db.Courses.Add(course);
                db.SaveChanges();
                
                // Send notification for course creation
                SendEntityNotification("Course", course.CourseID.ToString(), course.Title, EntityOperation.CREATE);
                
                return RedirectToAction("Index");
            }

            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            return View(course);
        }

        // GET: Courses/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("CourseID,Title,Credits,DepartmentID")] Course course, IFormFile teachingMaterialImage)
        {
            var courseToUpdate = db.Courses.Find(course.CourseID);
            if (courseToUpdate == null)
            {
                return NotFound();
            }

            course.TeachingMaterialImagePath = courseToUpdate.TeachingMaterialImagePath;
            if (ModelState.IsValid)
            {
                string oldFilePath = null;

                // Handle file upload if a new image is provided
                if (teachingMaterialImage != null && teachingMaterialImage.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var fileExtension = Path.GetExtension(teachingMaterialImage.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Please upload a valid image file (jpg, jpeg, png, gif, bmp).");
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    // Validate file size (max 5MB)
                    if (teachingMaterialImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "File size must be less than 5MB.");
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    try
                    {
                        // Create uploads directory if it doesn't exist
                        var uploadsPath = Path.Combine(_environment.WebRootPath, "Uploads", "TeachingMaterials");
                        Directory.CreateDirectory(uploadsPath);

                        oldFilePath = GetUploadedFilePath(courseToUpdate.TeachingMaterialImagePath);

                        // Generate unique filename
                        var fileName = $"course_{course.CourseID}_{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, fileName);

                        // Save new file
                        using (var stream = new FileStream(filePath, FileMode.CreateNew))
                        {
                            teachingMaterialImage.CopyTo(stream);
                        }
                        courseToUpdate.TeachingMaterialImagePath = $"~/Uploads/TeachingMaterials/{fileName}";
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Error uploading file.");
                        course.TeachingMaterialImagePath = courseToUpdate.TeachingMaterialImagePath;
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }
                }

                courseToUpdate.Title = course.Title;
                courseToUpdate.Credits = course.Credits;
                courseToUpdate.DepartmentID = course.DepartmentID;
                db.SaveChanges();

                if (oldFilePath != null && System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    catch (IOException)
                    {
                        System.Diagnostics.Debug.WriteLine("Unable to remove the previous course image.");
                    }
                }
                
                // Send notification for course update
                SendEntityNotification("Course", course.CourseID.ToString(), course.Title, EntityOperation.UPDATE);
                
                return RedirectToAction("Index");
            }
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            course.TeachingMaterialImagePath = courseToUpdate.TeachingMaterialImagePath;
            return View(course);
        }

        // GET: Courses/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = db.Courses.Include(c => c.Department).FirstOrDefault(c => c.CourseID == id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Course course = db.Courses.Find(id);
            var courseTitle = course.Title;
            
            // Delete associated image file if it exists
            if (!string.IsNullOrEmpty(course.TeachingMaterialImagePath))
            {
                var filePath = GetUploadedFilePath(course.TeachingMaterialImagePath);
                if (filePath != null && System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't prevent deletion of the course
                        // In a production application, you would log this error properly
                        System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
                    }
                }
            }
            
            db.Courses.Remove(course);
            db.SaveChanges();
            
            // Send notification for course deletion
            SendEntityNotification("Course", id.ToString(), courseTitle, EntityOperation.DELETE);
            
            return RedirectToAction("Index");
        }

        private string GetUploadedFilePath(string relativePath)
        {
            var fileName = Path.GetFileName(relativePath?.Replace('\\', '/'));
            return string.IsNullOrEmpty(fileName)
                ? null
                : Path.Combine(_environment.WebRootPath, "Uploads", "TeachingMaterials", fileName);
        }
    }
}
