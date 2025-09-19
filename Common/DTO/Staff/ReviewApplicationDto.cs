using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Staff
{
    public class ReviewApplicationDto
    {
        [Required(ErrorMessage = "ID đơn ứng tuyển là bắt buộc")]
        public Guid ApplicationId { get; set; }

        [Required(ErrorMessage = "Quyết định phê duyệt là bắt buộc")]
        public bool IsApproved { get; set; }

        [StringLength(500, ErrorMessage = "Lý do từ chối không được vượt quá 500 ký tự")]
        public string? RejectionReason { get; set; }
    }
}
