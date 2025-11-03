using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Refund
{
    public class RefundRequestDto
    {
        public Guid RefundRequestID { get; set; }
        public Guid EnrollmentID { get; set; }
        public Guid StudentID { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public Guid ClassID { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public RefundRequestType RequestType { get; set; }
        public string? Reason { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankAccountHolderName { get; set; } = string.Empty;
        public RefundRequestStatus Status { get; set; }
        public string? AdminNote { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProofImageUrl { get; set; } // URL hình ảnh chứng minh đã hoàn tiền
    }
}
