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

   
        public List<int> GoalIds { get; set; } = new();
        public List<string> GoalNames { get; set; } = new();

      
        public int? GoalID => GoalIds.FirstOrDefault() == 0 ? null : GoalIds.FirstOrDefault();
        public string GoalName => string.Join(", ", GoalNames);

        public string LanguageName { get; set; } = string.Empty;
        public List<VoiceAssessmentQuestion> Questions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int CurrentQuestionIndex { get; set; } = 0;
        public List<string> SubmittedAudioPaths { get; set; } = new();
    }
}
