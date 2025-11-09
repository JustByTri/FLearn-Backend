namespace Common.DTO.ProgressTracking.Response
{
    public class ProgressTrackingResponse
    {
        public Guid EnrollmentId { get; set; }
        public Guid UnitProgressId { get; set; }
        public Guid LessonProgressId { get; set; }
        public double LessonProgressPercent { get; set; }
        public double UnitProgressPercent { get; set; }
        public double EnrollmentProgressPercent { get; set; }
        public string LessonStatus { get; set; } = null!;
        public string UnitStatus { get; set; } = null!;
        public int TotalTimeSpentMinutes { get; set; }
        public string? LastAccessedAt { get; set; }
    }
}
