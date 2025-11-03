using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Refund
{
    /// <summary>
    /// DTO để Admin gửi email thông báo cho học viên
    /// </summary>
    public class RefundEmailDto
    {
        [Required]
        public Guid RefundRequestId { get; set; }

        [Required(ErrorMessage = "Tiêu đề email là bắt buộc")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung email là bắt buộc")]
        public string Body { get; set; } = string.Empty;
    }
}
