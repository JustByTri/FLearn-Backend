using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Auth
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email hoặc tên đăng nhập là bắt buộc")]
        public string EmailOrUsername { get; set; }
    }
}
