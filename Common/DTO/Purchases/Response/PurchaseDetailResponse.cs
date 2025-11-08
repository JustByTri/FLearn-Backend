namespace Common.DTO.Purchases.Response
{
    public class PurchaseDetailResponse
    {
        public Guid PurchaseId { get; set; }
        public Guid CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? CourseDescription { get; set; }
        public string? CourseThumbnail { get; set; }
        public decimal CoursePrice { get; set; }
        public decimal? CourseDiscountPrice { get; set; }
        public int CourseDurationDays { get; set; }
        public string? CourseLevel { get; set; }
        public string? CourseLanguage { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string? PurchaseStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CreatedAt { get; set; }
        public string? StartsAt { get; set; }
        public string? ExpiresAt { get; set; }
        public string? EligibleForRefundUntil { get; set; }
        public bool IsRefundEligible { get; set; }
        public int DaysRemaining { get; set; }
        public Guid? EnrollmentId { get; set; }
        public string? EnrollmentStatus { get; set; }
    }
}
