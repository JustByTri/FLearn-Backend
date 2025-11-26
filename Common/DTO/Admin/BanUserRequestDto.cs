using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class BanUserRequestDto
    {
        public Guid UserId { get; set; }
        public string Reason { get; set; } = "Vi phạm điều khoản sử dụng.";
    }
}
