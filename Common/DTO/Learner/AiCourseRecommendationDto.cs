using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Learner
{
    public class AiCourseRecommendationDto
    {
        public List<CourseRecommendationDto> RecommendedCourses { get; set; } = new();
        public string ReasoningExplanation { get; set; } = string.Empty;
        public string LearningPath { get; set; } = string.Empty;
        public List<string> StudyTips { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
