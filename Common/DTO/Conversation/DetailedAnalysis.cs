using System;
using System.Collections.Generic;

namespace Common.DTO.Conversation
{
    /// <summary>
    /// Phân tích chi ti?t t?ng k? n?ng
    /// </summary>
    public class DetailedSkillAnalysis
    {
        public string SkillName { get; set; } = string.Empty;
        public string QualitativeAssessment { get; set; } = string.Empty; // Mô t? chi ti?t
        public List<string> SpecificExamples { get; set; } = new();
        public List<string> SuggestedImprovements { get; set; } = new();
        public string CurrentLevel { get; set; } = string.Empty; // "Beginner", "Intermediate", "Advanced"
    }
    
    /// <summary>
    /// Quan sát c? th?
    /// </summary>
    public class SpecificObservation
    {
        public string Category { get; set; } = string.Empty; // "Grammar", "Vocabulary", "Fluency", etc.
        public string Observation { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty; // "Positive", "Needs improvement"
        public string? Example { get; set; }
    }
}
