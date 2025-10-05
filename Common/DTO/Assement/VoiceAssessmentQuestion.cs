using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceAssessmentQuestion
    {
        public int QuestionNumber { get; set; }
        public string Question { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;

      
        public string? VietnameseTranslation { get; set; }
        public List<WordWithGuide>? WordGuides { get; set; }

        public string QuestionType { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int MaxRecordingSeconds { get; set; }
        public bool IsSkipped { get; set; }
        public VoiceEvaluationResult? EvaluationResult { get; set; }
    }

 
    public class WordWithGuide
    {
        public string Word { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty; 
        public string VietnameseMeaning { get; set; } = string.Empty;
        public string? Example { get; set; } 
    }
}
