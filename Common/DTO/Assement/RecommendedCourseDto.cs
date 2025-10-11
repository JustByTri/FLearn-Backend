using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class RecommendedCourseDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string MatchReason { get; set; } = string.Empty;
        public string? GoalName { get; set; }
    }
}
