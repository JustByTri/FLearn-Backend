namespace Common.DTO.ExerciseGrading.Response
{
    public class ExerciseGradingAssignmentResponse
    {
        public Guid AssignmentId { get; set; }
        public Guid ExerciseSubmissionId { get; set; }
        public Guid LearnerId { get; set; }
        public string LearnerName { get; set; } = string.Empty;
        public Guid? AssignedTeacherId { get; set; }
        public string? AssignedTeacherName { get; set; }
        public Guid ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = string.Empty;
        public double PassScore { get; set; }
        public string ExerciseType { get; set; } = string.Empty;
        public Guid? LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public Guid? CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public double AIScore { get; set; }
        public string AIFeedback { get; set; } = string.Empty;
        public double TeacherScore { get; set; }
        public string TeacherFeedback { get; set; } = string.Empty;
        public double? FinalScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public string GradingStatus { get; set; } = string.Empty;
        public string EarningStatus { get; set; } = string.Empty;
        public decimal EarningAmount { get; set; }
        public string AssignedAt { get; set; } = string.Empty;
        public string Deadline { get; set; } = string.Empty;
        public string? StartedAt { get; set; }
        public string? CompletedAt { get; set; }
        public bool IsOverdue { get; set; }
        public int HoursRemaining { get; set; }
    }
}
