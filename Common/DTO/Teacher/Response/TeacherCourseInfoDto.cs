using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher.Response
{
    public class TeacherCourseInfoDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int LearnerCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
