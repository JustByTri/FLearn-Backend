using DAL.Type;

namespace Common.DTO.Purchases.Request
{
    public class PurchaseCourseRequest
    {
        public Guid CourseId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.PayOS;
        public string? PromotionCode { get; set; }
    }
}
