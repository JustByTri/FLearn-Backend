namespace Common.DTO.ExerciseGrading.Response
{
    public class ExerciseGradingStatusResponse
    {
        public Guid ExerciseSubmissionId { get; set; }
        public string Status { get; set; } = null!;
        public double? AIScore { get; set; }
        public double? TeacherScore { get; set; }
        public double? FinalScore { get; set; }
        public bool? IsPassed { get; set; }
        public string? AIFeedback { get; set; }
        public string? TeacherFeedback { get; set; }
        public string? SubmittedAt { get; set; }
        public string? ReviewedAt { get; set; }
        public Guid? AssignedTeacherId { get; set; }
        public string? AssignedTeacherName { get; set; }
        public string? AssignmentDeadline { get; set; }
    }
}
