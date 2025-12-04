using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Refund
{
    /// <summary>
    /// DTO cho admin yêu c?u h?c viên c?p nh?t l?i thông tin ngân hàng
    /// (Không reject ??n, ch? g?i thông báo)
    /// </summary>
    public class RequestBankUpdateDto
    {
        /// <summary>
        /// Ghi chú t? admin (lý do c?n c?p nh?t)
        /// Ví d?: "S? tài kho?n không ?úng ??nh d?ng", "Tên ch? TK sai chính t?"
        /// </summary>
        [Required(ErrorMessage = "Vui lòng cung c?p lý do yêu c?u c?p nh?t")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do ph?i t? 10-500 ký t?")]
        public string Note { get; set; } = string.Empty;
    }
}
