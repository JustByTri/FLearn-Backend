using System.ComponentModel.DataAnnotations;

namespace Common.DTO.LessonLog.Request
{
    public enum LessonLogTypeDto
    {
        ContentRead = 1,
        VideoProgress = 2,
        PdfOpened = 3,
        ExercisePassed = 4,
        ExerciseFailed = 5,
        Generic = 99
    }
    public class LessonLogRequest
    {
        [Required]
        public Guid LessonId { get; set; }
        [Required]
        public Guid EnrollmentId { get; set; }
        [Required]
        public LessonLogTypeDto Type { get; set; }
        public double? Value { get; set; }
        public string? MetadataJson { get; set; }
    }
}
