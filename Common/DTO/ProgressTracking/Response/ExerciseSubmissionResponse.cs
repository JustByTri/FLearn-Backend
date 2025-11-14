namespace Common.DTO.ProgressTracking.Response
{
    public class ExerciseSubmissionResponse
    {
        public Guid ExerciseSubmissionId { get; set; }
        public Guid ExerciseId { get; set; }
        public double? AIScore { get; set; }
        public string? AIFeedback { get; set; }
        public double? TeacherScore { get; set; }
        public string? TeacherFeedback { get; set; }
        public double? FinalScore { get; set; }
        public bool? IsPassed { get; set; }
        public string Status { get; set; } = null!;
        public string? SubmittedAt { get; set; }
    }
}
