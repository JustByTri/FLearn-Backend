namespace Common.DTO.ExerciseSubmission.Response
{
    public class ExerciseSubmissionDetailResponse
    {
        public Guid ExerciseSubmissionId { get; set; }
        public Guid ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = null!;
        public string ExerciseDescription { get; set; } = null!;
        public string ExerciseType { get; set; } = null!;
        public double PassScore { get; set; }
        public string AudioUrl { get; set; } = null!;
        public string? SubmittedAt { get; set; }
        public string Status { get; set; } = null!;
        public double? AIScore { get; set; }
        public string? AIFeedback { get; set; }
        public double? TeacherScore { get; set; }
        public string? TeacherFeedback { get; set; }
        public double? FinalScore { get; set; }
        public bool? IsPassed { get; set; }
        public string? ReviewedAt { get; set; }
        public Guid LessonId { get; set; }
        public string LessonTitle { get; set; } = null!;
        public Guid UnitId { get; set; }
        public string UnitTitle { get; set; } = null!;
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherAvatar { get; set; }
    }
    public class ExerciseSubmissionHistoryResponse
    {
        public Guid ExerciseSubmissionId { get; set; }
        public double AIScore { get; set; }
        public double AIPercent { get; set; }
        public string? AIFeedback { get; set; }
        public double TeacherScore { get; set; }
        public double TeacherPercent { get; set; }
        public string? TeacherFeedback { get; set; }
        public double? FinalScore { get; set; }
        public double PassScore { get; set; }
        public bool? IsPassed { get; set; }
        public string Status { get; set; } = null!;
        public string AudioUrl { get; set; } = null!;
        public string? SubmittedAt { get; set; }
    }
    public class GradingCriteriaScore
    {
        public string Criteria { get; set; } = null!;
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public string Feedback { get; set; } = null!;
    }
}
