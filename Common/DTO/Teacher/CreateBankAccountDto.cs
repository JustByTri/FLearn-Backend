using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class CreateBankAccountDto
    {
        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc")]
        [StringLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chi nhánh là bắt buộc")]
        [StringLength(100)]
        public string BankBranch { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tài khoản là bắt buộc")]
        [StringLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên chủ tài khoản là bắt buộc")]
        [StringLength(100)]
        public string AccountHolderName { get; set; } = string.Empty;
    }
}
