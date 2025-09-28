namespace Common.DTO.Exercise.Response
{
    public class ExerciseResponse
    {
        public Guid ExerciseID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Prompt { get; set; }
        public string? Hints { get; set; }
        public string? Content { get; set; }
        public string? ExpectedAnswer { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaPublicId { get; set; }
        public int Position { get; set; }
        public string? ExerciseType { get; set; }
        public string? SkillType { get; set; }
        public string? Difficulty { get; set; }
        public int MaxScore { get; set; }
        public int PassScore { get; set; }
        public string? FeedbackCorrect { get; set; }
        public string? FeedbackIncorrect { get; set; }
        public Guid? PrerequisiteExerciseID { get; set; }
        public Guid? CourseID { get; set; }
        public string? CourseTitle { get; set; }
        public Guid? UnitID { get; set; }
        public string? UnitTitle { get; set; }
        public Guid LessonID { get; set; }
        public string? LessonTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ExerciseOptionResponse>? Options { get; set; }
    }
}
