using System.ComponentModel.DataAnnotations;

namespace Common.DTO.CourseTemplate.Request
{
    public class CourseTemplateRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name must be less than 200 characters")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description must be less than 500 characters")]
        public string? Description { get; set; }

        public bool RequireGoal { get; set; }
        public bool RequireLevel { get; set; }
        public bool RequireSkillFocus { get; set; }
        public bool RequireTopic { get; set; }
        public bool RequireLang { get; set; }

        [Range(0, 100, ErrorMessage = "MinUnits must be >= 0 and <= 100")]
        public int MinUnits { get; set; }

        [Range(0, 100, ErrorMessage = "MinLessonsPerUnit must be >= 0 and <= 100")]
        public int MinLessonsPerUnit { get; set; }

        [Range(0, 100, ErrorMessage = "MinExercisesPerLesson must be >= 0 and <= 100")]
        public int MinExercisesPerLesson { get; set; }
    }
}
