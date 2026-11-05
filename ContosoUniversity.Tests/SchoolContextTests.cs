using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Tests
{
    public class SchoolContextTests
    {
        private static SchoolContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new SchoolContext(options);
        }

        [Fact]
        public void DbInitializer_Initialize_SeedsStudentsCoursesDepartmentsAndInstructors()
        {
            using var context = CreateContext(nameof(DbInitializer_Initialize_SeedsStudentsCoursesDepartmentsAndInstructors));

            DbInitializer.Initialize(context);

            Assert.True(context.Students.Any());
            Assert.True(context.Courses.Any());
            Assert.True(context.Departments.Any());
            Assert.True(context.Instructors.Any());
        }

        [Fact]
        public void DbInitializer_Initialize_IsIdempotent_WhenCalledTwice()
        {
            using var context = CreateContext(nameof(DbInitializer_Initialize_IsIdempotent_WhenCalledTwice));

            DbInitializer.Initialize(context);
            var studentCountAfterFirstRun = context.Students.Count();

            DbInitializer.Initialize(context);
            var studentCountAfterSecondRun = context.Students.Count();

            Assert.Equal(studentCountAfterFirstRun, studentCountAfterSecondRun);
        }

        [Fact]
        public void Department_Course_Relationship_IsNavigable()
        {
            using var context = CreateContext(nameof(Department_Course_Relationship_IsNavigable));
            DbInitializer.Initialize(context);

            var course = context.Courses
                .Include(c => c.Department)
                .First();

            Assert.NotNull(course.Department);
        }

        [Fact]
        public void Instructor_CourseAssignments_And_OfficeAssignment_AreNavigable()
        {
            using var context = CreateContext(nameof(Instructor_CourseAssignments_And_OfficeAssignment_AreNavigable));
            DbInitializer.Initialize(context);

            var instructorWithCourses = context.Instructors
                .Include(i => i.CourseAssignments)
                    .ThenInclude(ca => ca.Course)
                .First(i => i.CourseAssignments.Any());

            Assert.NotEmpty(instructorWithCourses.CourseAssignments);
            Assert.All(instructorWithCourses.CourseAssignments, ca => Assert.NotNull(ca.Course));
        }

        [Fact]
        public void Student_Enrollments_AreNavigable()
        {
            using var context = CreateContext(nameof(Student_Enrollments_AreNavigable));
            DbInitializer.Initialize(context);

            var studentWithEnrollments = context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .First(s => s.Enrollments.Any());

            Assert.NotEmpty(studentWithEnrollments.Enrollments);
        }
    }
}
