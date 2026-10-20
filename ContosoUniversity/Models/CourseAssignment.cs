using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ContosoUniversity.Models
{
    public class CourseAssignment
    {
        public int InstructorID { get; set; }
        public int CourseID { get; set; }

        [ValidateNever]
        public virtual Instructor Instructor { get; set; } = null!;
        [ValidateNever]
        public virtual Course Course { get; set; } = null!;
    }
}
