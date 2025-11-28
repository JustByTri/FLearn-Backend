namespace Common.DTO.LessonProgress.Response
{
    public class LessonExerciseProgressResponse
    {
        public Guid ExerciseID { get; set; }
        public string? Title { get; set; }
        public string? Prompt { get; set; }
        public string? Hints { get; set; }
        public string? Content { get; set; }
        public string? ExpectedAnswer { get; set; }
        public string[]? MediaUrls { get; set; }
        public string[]? MediaPublicIds { get; set; }
        public int Position { get; set; }
        public string? ExerciseType { get; set; }
        public string? Difficulty { get; set; }
        public int MaxScore { get; set; }
        public int PassScore { get; set; }
        public string? FeedbackCorrect { get; set; }
        public string? FeedbackIncorrect { get; set; }
        public Guid LessonID { get; set; }
        public string? LessonTitle { get; set; }
        public Guid? CourseID { get; set; }
        public string? CourseTitle { get; set; }
        public Guid? UnitID { get; set; }
        public string? UnitTitle { get; set; }
        public Guid? SubmissionId { get; set; }
        public string SubmissionStatus { get; set; } = "NotStarted";
        public double? Score { get; set; }
        public bool? IsPassed { get; set; }
        public string? SubmittedAt { get; set; }
        public string? ReviewedAt { get; set; }
        public string? AIFeedback { get; set; }
        public string? TeacherFeedback { get; set; }
        public bool IsCurrent { get; set; }
    }
}
