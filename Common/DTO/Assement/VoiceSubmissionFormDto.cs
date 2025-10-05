using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceSubmissionFormDto
    {
       
        [Required(ErrorMessage = "Số câu hỏi là bắt buộc")]
        [Range(1, 10, ErrorMessage = "Số câu hỏi phải từ 1 đến 10")]
        public int QuestionNumber { get; set; }

        [Required(ErrorMessage = "Trạng thái bỏ qua là bắt buộc")]
        public bool IsSkipped { get; set; }

        public IFormFile? AudioFile { get; set; }

        public int RecordingDurationSeconds { get; set; } = 0;
    }
}
