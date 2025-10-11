using DAL.Type;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class SendMessageRequestDto
    {
        [Required(ErrorMessage = "Session ID là bắt buộc")]
        public Guid SessionId { get; set; }

        [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc")]
        [StringLength(2000, ErrorMessage = "Tin nhắn không được vượt quá 2000 ký tự")]
        public string MessageContent { get; set; } = string.Empty;

        public MessageType MessageType { get; set; } = MessageType.Text;

    
        public string? AudioUrl { get; set; }
        public string? AudioPublicId { get; set; }
        public int? AudioDuration { get; set; }
    }
}
