namespace Common.DTO.LessonLog.Response
{
    public class LessonLogResponse
    {
        public Guid LessonActivityLogId { get; set; }
        public Guid LessonId { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid LearnerId { get; set; }
        public string ActivityType { get; set; } = "";
        public double? Value { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
