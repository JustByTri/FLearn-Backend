namespace Common.DTO.Course.Response
{
    public class CourseAccessResponse
    {
        public bool HasAccess { get; set; }
        public string? ExpiresAt { get; set; }
        public int DaysRemaining { get; set; }
        public string? RefundEligibleUntil { get; set; }
        public string AccessStatus { get; set; } = null!;
        public Guid? PurchaseId { get; set; }
        public Guid? EnrollmentId { get; set; }
    }
}
