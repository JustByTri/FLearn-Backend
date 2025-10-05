using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceEvaluationResult
    {
        public int OverallScore { get; set; }
        public PronunciationScore Pronunciation { get; set; } = new();
        public FluencyScore Fluency { get; set; } = new();
        public GrammarScore Grammar { get; set; } = new();
        public VocabularyScore Vocabulary { get; set; } = new();
        public string DetailedFeedback { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> AreasForImprovement { get; set; } = new();
    }
    public class PronunciationScore
    {
        public int Score { get; set; } 
        public string Level { get; set; } = string.Empty;
        public List<string> MispronuncedWords { get; set; } = new();
        public string Feedback { get; set; } = string.Empty;
    }

    public class FluencyScore
    {
        public int Score { get; set; }
        public double SpeakingRate { get; set; }
        public int PauseCount { get; set; }
        public string Rhythm { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
    }

    public class GrammarScore
    {
        public int Score { get; set; } 
        public List<string> GrammarErrors { get; set; } = new();
        public string StructureAssessment { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
    }

    public class VocabularyScore
    {
        public int Score { get; set; } 
        public string RangeAssessment { get; set; } = string.Empty; 
        public string AccuracyAssessment { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
    }
}
