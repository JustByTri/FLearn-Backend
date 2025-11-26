namespace Common.DTO.Pronunciation
{
    public class PronunciationAssessmentResult
    {
        public float AccuracyScore { get; set; }
        public float FluencyScore { get; set; }
        public float CompletenessScore { get; set; }
        public float PronunciationScore { get; set; }
        public string RecognizedText { get; set; } = string.Empty;
        public List<PhonemeAssessment> PhonemeAssessments { get; set; } = new();
    }
    public class PhonemeAssessment
    {
        public string Phoneme { get; set; } = string.Empty;
        public float AccuracyScore { get; set; }
        public int Offset { get; set; }
        public int Duration { get; set; }
    }
}
