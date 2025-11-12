namespace Common.DTO.LessonProgress.Response
{
    public class LessonExerciseProgressResponse
    {
        public Guid ExerciseId { get; set; }
        public string Title { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double PassScore { get; set; }
        public Guid? SubmissionId { get; set; }
        public string SubmissionStatus { get; set; } = null!;
        public double? Score { get; set; }
        public bool? IsPassed { get; set; }
        public string? SubmittedAt { get; set; }
        public string? ReviewedAt { get; set; }
        public string? AIFeedback { get; set; }
        public string? TeacherFeedback { get; set; }
        public int Order { get; set; }
        public bool IsCurrent { get; set; }
    }
}
