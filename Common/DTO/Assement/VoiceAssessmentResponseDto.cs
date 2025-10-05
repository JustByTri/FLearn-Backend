using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceAssessmentResponseDto
    {
        public Guid AssessmentId { get; set; }
        public int QuestionNumber { get; set; }
        public IFormFile? AudioFile { get; set; } 
        public bool IsSkipped { get; set; }
        public int RecordingDurationSeconds { get; set; }
    }
}
