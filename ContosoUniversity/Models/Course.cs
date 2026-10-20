using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ContosoUniversity.Models
{
    public class Course
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Number")]
        public int CourseID { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Range(0, 5)]
        public int Credits { get; set; }

        public int DepartmentID { get; set; }

        [Display(Name = "Teaching Material Image")]
        [StringLength(255)]
        public string? TeachingMaterialImagePath { get; set; }

        [ValidateNever]
        public virtual Department Department { get; set; } = null!;
        [ValidateNever]
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        [ValidateNever]
        public virtual ICollection<CourseAssignment> CourseAssignments { get; set; } = new List<CourseAssignment>();
    }
}
