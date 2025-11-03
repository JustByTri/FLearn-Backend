using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Refund
{
    /// <summary>
    /// DTO ?? Admin x? lý ??n hoàn ti?n (Approve ho?c Reject)
    /// </summary>
    public class ProcessRefundRequestDto
    {
     [Required(ErrorMessage = "ID ??n hoàn ti?n là b?t bu?c")]
      public Guid RefundRequestId { get; set; }

        [Required(ErrorMessage = "Tr?ng thái x? lý là b?t bu?c")]
        public ProcessAction Action { get; set; }

      [StringLength(500, ErrorMessage = "Ghi chú không ???c v??t quá 500 ký t?")]
        public string? AdminNote { get; set; }

        /// <summary>
     /// Hình ?nh ch?ng minh ?ã hoàn ti?n (n?u Approve)
/// </summary>
        public IFormFile? ProofImage { get; set; }
    }

    public enum ProcessAction
    {
   Approve = 1,    // Ch?p nh?n và hoàn ti?n
        Reject = 2   // T? ch?i
    }
}
