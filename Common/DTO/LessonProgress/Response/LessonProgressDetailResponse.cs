namespace Common.DTO.LessonProgress.Response
{
    public class LessonProgressDetailResponse
    {
        public Guid LessonProgressId { get; set; }
        public Guid LessonId { get; set; }
        public string LessonTitle { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double ProgressPercent { get; set; }
        public string Status { get; set; } = null!;
        public string StartedAt { get; set; }
        public string? CompletedAt { get; set; }
        public string? LastUpdated { get; set; }
        public LessonActivityStatusResponse ActivityStatus { get; set; } = null!;
        public int TotalExercises { get; set; }
        public int CompletedExercises { get; set; }
        public int PassedExercises { get; set; }
        public Guid UnitId { get; set; }
        public string UnitTitle { get; set; } = null!;
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public Guid? PreviousLessonId { get; set; }
        public string? PreviousLessonTitle { get; set; }
        public Guid? NextLessonId { get; set; }
        public string? NextLessonTitle { get; set; }
        public List<string> CompletionRequirements { get; set; } = new();
    }

}
