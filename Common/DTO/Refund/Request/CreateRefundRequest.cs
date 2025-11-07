using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Refund.Request
{
    public class CreateRefundRequest
    {
        [Required(ErrorMessage = "PurchaseId là bắt buộc")]
        public Guid PurchaseId { get; set; }

        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên ngân hàng không được vượt quá 100 ký tự")]
        public string? BankName { get; set; }
        [Required(ErrorMessage = "Số tài khoản ngân hàng là bắt buộc")]
        [RegularExpression(@"^\d{9,16}$", ErrorMessage = "Số tài khoản phải từ 9-16 chữ số")]
        public string? BankAccountNumber { get; set; }

        [Required(ErrorMessage = "Tên chủ tài khoản là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên chủ tài khoản không được vượt quá 100 ký tự")]
        public string? BankAccountHolderName { get; set; }
        [StringLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự")]
        public string? Reason { get; set; }
    }
}
