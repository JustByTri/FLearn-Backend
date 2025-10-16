namespace Common.DTO.LessonProgress.Response
{
    public class LearnerProgressResponse
    {
        public Guid LearnerProgressId { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid LessonId { get; set; }
        public bool IsCompleted { get; set; }
        public double ProgressPercent { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
