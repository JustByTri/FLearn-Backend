using System.ComponentModel.DataAnnotations;

namespace Common.DTO.CourseTemplate.Request
{
    public class CourseTemplateRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name must be less than 200 characters")]
        public string? Name { get; set; }
        [StringLength(500, ErrorMessage = "Description must be less than 500 characters")]
        public string? Description { get; set; }
        [Range(1, 100, ErrorMessage = "UnitCount must be >= 0 and <= 100")]
        public int UnitCount { get; set; }
        [Range(1, 100, ErrorMessage = "LessonsPerUnit must be >= 1 and <= 100")]
        public int LessonsPerUnit { get; set; }
        [Range(1, 100, ErrorMessage = "ExercisesPerLesson must be >= 1 and <= 100")]
        public int ExercisesPerLesson { get; set; }
        [Required(ErrorMessage = "ProgramId is required")]
        public Guid ProgramId { get; set; }
        [Required(ErrorMessage = "LevelId is required")]
        public Guid LevelId { get; set; }
    }
}
