using System.Collections.Generic;
using System.Linq;

namespace ContosoUniversity.Models.SchoolViewModels
{
    public class InstructorIndexData
    {
        public IEnumerable<Instructor> Instructors { get; set; } = Enumerable.Empty<Instructor>();
        public IEnumerable<Course> Courses { get; set; } = Enumerable.Empty<Course>();
        public IEnumerable<Enrollment> Enrollments { get; set; } = Enumerable.Empty<Enrollment>();
    }
}
