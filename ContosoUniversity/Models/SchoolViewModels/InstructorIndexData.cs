using System.Collections.Generic;

namespace ContosoUniversity.Models.SchoolViewModels
{
    public class InstructorIndexData
    {
        public IEnumerable<Instructor> Instructors { get; set; } = new List<Instructor>();
        public IEnumerable<Course>? Courses { get; set; }
        public IEnumerable<Enrollment>? Enrollments { get; set; }
    }
}
