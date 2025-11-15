namespace Common.DTO.Enrollment.Response
{
    public class ContinueLearningResponse
    {
        public Guid EnrollmentId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public string CourseImage { get; set; } = null!;
        public double ProgressPercent { get; set; }
        public ContinueLessonDto? ContinueLesson { get; set; }
        public string LastAccessed { get; set; } = null!; // "2 hours ago", "yesterday"
    }
    public class ContinueLessonDto
    {
        public Guid LessonId { get; set; }
        public string Title { get; set; } = null!;
        public double ProgressPercent { get; set;  }
    }
}
