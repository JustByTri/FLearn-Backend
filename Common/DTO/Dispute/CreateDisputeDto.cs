using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Dispute
{
    /// <summary>
    /// DTO ?? h?c viên t?o ??n khi?u n?i sau khi h?c xong l?p
    /// </summary>
    public class CreateDisputeDto
    {
        /// <summary>
        /// ID c?a enrollment (??ng ký l?p h?c)
        /// </summary>
        [Required(ErrorMessage = "EnrollmentId là b?t bu?c")]
        public Guid EnrollmentId { get; set; }

        /// <summary>
        /// Lý do khi?u n?i ng?n g?n
        /// </summary>
        [Required(ErrorMessage = "Lý do khi?u n?i là b?t bu?c")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do ph?i t? 10-500 ký t?")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Mô t? chi ti?t v? v?n ?? g?p ph?i
        /// </summary>
        [StringLength(2000, ErrorMessage = "Mô t? không ???c v??t quá 2000 ký t?")]
        public string? Description { get; set; }
    }
}
