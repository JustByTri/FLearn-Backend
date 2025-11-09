namespace Common.DTO.ExerciseGrading.Response
{
    public class ExerciseGradingAssignmentResponse
    {
        public Guid AssignmentId { get; set; }
        public Guid ExerciseSubmissionId { get; set; }
        public Guid LearnerId { get; set; }
        public string LearnerName { get; set; } = null!;
        public Guid ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = null!;
        public string AudioUrl { get; set; } = null!;
        public double? AIScore { get; set; }
        public string? AIFeedback { get; set; }
        public string Status { get; set; } = null!;
        public string? AssignedAt { get; set; }
        public string? Deadline { get; set; }
        public bool IsOverdue { get; set; }
        public int HoursRemaining { get; set; }
    }
}
