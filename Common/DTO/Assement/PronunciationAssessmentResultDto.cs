using System;
using System.Collections.Generic;

namespace Common.DTO.Assement
{
    public class PronunciationAssessmentResultDto
    {
        public double Accuracy { get; set; }
        public double Fluency { get; set; }
        public double Completeness { get; set; }
        public double Pronunciation { get; set; }
        public List<WordPronunciationScoreDto> Words { get; set; } = new();
        public string? RawJson { get; set; }
    }

    public class WordPronunciationScoreDto
    {
        public string Word { get; set; } = string.Empty;
        public double Accuracy { get; set; }
        public bool IsInserted { get; set; }
        public bool IsOmitted { get; set; }
        public bool IsSubstituted { get; set; }
    }
}
