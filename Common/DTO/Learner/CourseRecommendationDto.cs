namespace Common.DTO.Learner
{
    public class CourseRecommendationDto
    {
        public Guid CourseID { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public decimal MatchScore { get; set; } // 0-100
        public string MatchReason { get; set; } = string.Empty;
        public int EstimatedDuration { get; set; } // in hours
        public Guid UserId { get; set; }
    }
}
