namespace Common.DTO.Purchases.Response
{
    public class PurchaseCourseResponse
    {
        public Guid PurchaseId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string? StartsAt { get; set; }
        public string? ExpiresAt { get; set; }
        public string? PurchaseStatus { get; set; }
    }
}
