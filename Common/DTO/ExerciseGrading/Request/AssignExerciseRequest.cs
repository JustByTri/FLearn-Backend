namespace Common.DTO.ExerciseGrading.Request
{
    public class AssignExerciseRequest
    {
        public Guid ExerciseSubmissionId { get; set; }
        public Guid TeacherId { get; set; }
    }
}
