using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceAssessmentDto
    {
        public Guid AssessmentId { get; set; }
        public Guid UserId { get; set; }
        public Guid LanguageId { get; set; }
        public int? GoalID { get; set; }
        public string GoalName { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public List<VoiceAssessmentQuestion> Questions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int CurrentQuestionIndex { get; set; } = 0;
        public List<string> SubmittedAudioPaths { get; set; } = new();
    }
}
