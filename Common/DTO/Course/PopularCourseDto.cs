using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Course
{
    public class PopularCourseDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int LearnerCount { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public string ProficiencyCode { get; set; } = string.Empty;
    }
}
