using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class SendVoiceMessageFormDto
    {
        [Required(ErrorMessage = "Session ID là bắt buộc")]
        public Guid SessionId { get; set; }

        [Required(ErrorMessage = "File audio là bắt buộc")]
        public IFormFile AudioFile { get; set; } = null!;

        /// <summary>
        /// Thời lượng audio (giây)
        /// </summary>
        public int? AudioDuration { get; set; }

        /// <summary>
        /// Optional: Transcript của voice message (nếu có speech-to-text)
        /// </summary>
        public string? Transcript { get; set; }
    }
}
