using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.PayOut
{
    public class CreatePayoutRequestDto
    {
        [Required]
        [Range(50000, 10000000, ErrorMessage = "Số tiền rút phải từ 50,000 đến 10,000,000 VNĐ")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tài khoản ngân hàng")]
        public Guid BankAccountId { get; set; }
    }
}
