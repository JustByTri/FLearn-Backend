namespace Common.DTO.Enrollment.Response
{
    public class EnrollmentResponse
    {
        public Guid EnrollmentId { get; set; }
        public Guid CourseId { get; set; }
        public string? CourseTitle { get; set; }
        public string? CourseType { get; set; }
        public decimal PricePaid { get; set; }
        public double ProgressPercent { get; set; }
        public string? EnrollmentDate { get; set; }
        public string? AccessUntil { get; set; }
        public string? EligibleForRefundUntil { get; set; }
        public string? LastAccessedAt { get; set; }
        public string? Status { get; set; }
    }
}
