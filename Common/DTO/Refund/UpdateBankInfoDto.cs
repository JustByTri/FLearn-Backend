using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Refund
{
    /// <summary>
    /// DTO ?? h?c viên c?p nh?t thông tin ngân hàng cho ??n hoàn ti?n
    /// (Sau khi l?p b? h?y, h?c viên c?n ?i?n thông tin ?? nh?n ti?n)
    /// </summary>
    public class UpdateBankInfoDto
    {
        /// <summary>
        /// Tên ngân hàng (VD: Vietcombank, BIDV, Techcombank...)
        /// </summary>
        [Required(ErrorMessage = "Tên ngân hàng là b?t bu?c")]
        [StringLength(100, ErrorMessage = "Tên ngân hàng không ???c v??t quá 100 ký t?")]
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// S? tài kho?n ngân hàng (9-16 ch? s?)
        /// </summary>
        [Required(ErrorMessage = "S? tài kho?n là b?t bu?c")]
        [RegularExpression(@"^\d{9,16}$", ErrorMessage = "S? tài kho?n ph?i t? 9-16 ch? s?")]
        public string BankAccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Tên ch? tài kho?n (Ph?i trùng v?i CMND/CCCD)
        /// </summary>
        [Required(ErrorMessage = "Tên ch? tài kho?n là b?t bu?c")]
        [StringLength(100, ErrorMessage = "Tên ch? tài kho?n không ???c v??t quá 100 ký t?")]
        public string BankAccountHolderName { get; set; } = string.Empty;
    }
}
