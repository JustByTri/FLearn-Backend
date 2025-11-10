namespace Common.DTO.Enrollment.Response
{
    public class EnrolledCourseCurriculumResponse
    {
        public Guid EnrollmentId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public List<CurriculumUnitDto> Units { get; set; } = new();
    }
    public class CurriculumUnitDto
    {
        public Guid UnitId { get; set; }
        public string Title { get; set; } = null!;
        public int Order { get; set; }
        public double ProgressPercent { get; set; }
        public string Status { get; set; } = null!; // NotStarted, InProgress, Completed, Locked
        public string? CompletedAt { get; set; }
        public List<CurriculumLessonDto> Lessons { get; set; } = new();
    }
    public class CurriculumLessonDto
    {
        public Guid LessonId { get; set; }
        public string Title { get; set; } = null!;
        public int Order { get; set; }
        public double ProgressPercent { get; set; }
        public string Status { get; set; } = null!; // NotStarted, InProgress, Completed
        public bool HasContent { get; set; }
        public bool HasVideo { get; set; }
        public bool HasDocument { get; set; }
        public bool HasExercise { get; set; }
    }
}
