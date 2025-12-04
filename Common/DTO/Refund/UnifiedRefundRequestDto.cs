using DAL.Models;

namespace Common.DTO.Refund
{
    /// <summary>
    /// DTO th?ng nh?t cho ??n hoàn ti?n - Bao g?m c? Class và Course
    /// </summary>
    public class UnifiedRefundRequestDto
    {
        public Guid RefundRequestID { get; set; }
        
        // Lo?i ??n hoàn ti?n
        public string RefundCategory { get; set; } = string.Empty; // "Class" ho?c "Course"
        
        // Thông tin h?c viên
        public Guid StudentID { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string? StudentAvatar { get; set; }
        
        // Thông tin khoá h?c/l?p h?c
        public Guid? ClassID { get; set; }
        public string? ClassName { get; set; }
        public Guid? PurchaseId { get; set; }
        public string? CourseName { get; set; }
        
        // Thông tin ??n hoàn ti?n
        public RefundRequestType RequestType { get; set; }
        public string? Reason { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankAccountHolderName { get; set; } = string.Empty;
        public RefundRequestStatus Status { get; set; }
        public string? AdminNote { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal OriginalAmount { get; set; }
        
        // Thông tin th?i gian
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        
        // Thông tin b? sung
        public string? ProofImageUrl { get; set; }
        public string? ProcessedByAdminName { get; set; }
        
        // Meta data ?? d? hi?n th?
        public string DisplayTitle { get; set; } = string.Empty; // Tên hi?n th? (Class ho?c Course)
        public string StatusText { get; set; } = string.Empty;
        public string RequestTypeText { get; set; } = string.Empty;
    }
}
