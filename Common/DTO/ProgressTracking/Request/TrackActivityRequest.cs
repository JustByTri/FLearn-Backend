using DAL.Type;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.ProgressTracking.Request
{
    public class TrackActivityRequest
    {
        [Required]
        public Guid LessonId { get; set; }
        [Required]
        public LessonLogType LogType { get; set; }
        public double? DurationMinutes { get; set; }
        public string? Metadata { get; set; }
    }
}
