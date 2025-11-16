using DAL.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Purchases.Response
{
    public class SubscriptionPurchaseResponse
    {
        public Guid PurchaseId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public int ConversationQuota { get; set; }
        public decimal Price { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CreatedAt { get; set; }
        public string? PaidAt { get; set; }
        public string? StartsAt { get; set; }
        public string? ExpiresAt { get; set; }
        public string? EligibleForRefundUntil { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsRefundEligible { get; set; }
        public bool IsActive { get; set; }
        public SubscriptionDetailResponse? SubscriptionDetails { get; set; }
    }
    public class SubscriptionDetailResponse
    {
        public Guid SubscriptionId { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public int ConversationQuota { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public bool IsActive { get; set; }
        public int ConversationsUsed { get; set; }
        public int ConversationsRemaining { get; set; }
    }
}
