using System.ComponentModel.DataAnnotations;

namespace Common.DTO.ProgressTracking.Request
{
    public class StartLessonRequest
    {
        [Required]
        public Guid LessonId { get; set; }
        [Required]
        public Guid UnitId { get; set; }
    }
}
