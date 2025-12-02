using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Admin
{
    /// <summary>
    /// Request DTO ?? Manager duy?t yêu c?u h?y l?p
    /// </summary>
    public class ApproveCancellationRequestDto
    {
        /// <summary>
        /// Ghi chú t? Manager (optional)
        /// </summary>
        [StringLength(1000, ErrorMessage = "Ghi chú không ???c v??t quá 1000 ký t?")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request DTO ?? Manager t? ch?i yêu c?u h?y l?p
    /// </summary>
    public class RejectCancellationRequestDto
    {
        /// <summary>
        /// Lý do t? ch?i (b?t bu?c)
        /// </summary>
        [Required(ErrorMessage = "Vui lòng cung c?p lý do t? ch?i")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Lý do t? ch?i ph?i t? 10-1000 ký t?")]
        public string Reason { get; set; } = string.Empty;
    }
}
