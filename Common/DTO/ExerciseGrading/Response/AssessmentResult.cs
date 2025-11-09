namespace Common.DTO.ExerciseGrading.Response
{
    public class AssessmentResult
    {
        public ExtendedScores Scores { get; set; } = new ExtendedScores();
        public string CefrLevel { get; set; } = "A1";
        public string? JlptLevel { get; set; }
        public string? HskLevel { get; set; }
        public int Overall { get; set; }
        public string Feedback { get; set; } = "";
        public string Transcript { get; set; } = "";
    }
}
