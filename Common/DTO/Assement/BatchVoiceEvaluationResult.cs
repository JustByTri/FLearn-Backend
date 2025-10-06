using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class BatchVoiceEvaluationResult
    {
        public string OverallLevel { get; set; } = "";
        public int OverallScore { get; set; }
        public List<QuestionEvaluationResult> QuestionResults { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<CourseRecommendation> RecommendedCourses { get; set; } = new();
        public DateTime EvaluatedAt { get; set; }
    }

    public class QuestionEvaluationResult
    {
        public int QuestionNumber { get; set; }
        public List<string> SpokenWords { get; set; } = new();
        public List<string> MissingWords { get; set; } = new();
        public int AccuracyScore { get; set; }
        public int PronunciationScore { get; set; }
        public int FluencyScore { get; set; }
        public int GrammarScore { get; set; }
        public string Feedback { get; set; } = "";
    }

    public class CourseRecommendation
    {
        public Guid? CourseId { get; set; } 
        public string Focus { get; set; } = ""; 
        public string Reason { get; set; } = "";
        public string Level { get; set; } = "";
    }
}
