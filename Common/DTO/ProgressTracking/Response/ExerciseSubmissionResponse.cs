namespace Common.DTO.ProgressTracking.Response
{
    public class ExerciseSubmissionResponse
    {
        public Guid ExerciseSubmissionId { get; set; }
        public Guid ExerciseId { get; set; }
        public string Status { get; set; } = null!;
        public double? AIScore { get; set; }
        public double? TeacherScore { get; set; }
        public double? FinalScore { get; set; }
        public bool? IsPassed { get; set; }
        public string? SubmittedAt { get; set; }
    }
}
