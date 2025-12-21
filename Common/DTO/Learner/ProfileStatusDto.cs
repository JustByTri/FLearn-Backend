namespace Common.DTO.Learner
{
    public class ProfileStatusDto
    {
        public Guid UserId { get; set; }
        public Guid? ActiveLanguageId { get; set; }
        public string? ActiveLanguageName { get; set; }
        public bool IsDoingAssessment { get; set; }
        public string? AssessmentStep { get; set; }
    }
}
