using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher.Response
{
    public class PublicTeacherProfileDto
    {
        public Guid TeacherId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Bio { get; set; }

        
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        
        public List<TeacherCourseInfoDto> PublishedCourses { get; set; } = new List<TeacherCourseInfoDto>();
    }
}
