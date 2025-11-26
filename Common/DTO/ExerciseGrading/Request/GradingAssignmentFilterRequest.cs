namespace Common.DTO.ExerciseGrading.Request
{
    public class GradingAssignmentFilterRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Status { get; set; }
        public Guid? AssignedTeacherId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? ExerciseId { get; set; }
        public Guid? CourseId { get; set; }
    }
}
