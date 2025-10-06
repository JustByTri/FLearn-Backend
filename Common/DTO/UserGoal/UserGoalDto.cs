using Common.DTO.Assement;

public class UserGoalDto
{
    public Guid UserGoalID { get; set; }
    public Guid UserID { get; set; }
    public Guid LanguageID { get; set; }
    public string LanguageName { get; set; } = string.Empty;

    public int? GoalId { get; set; }
    public string? GoalName { get; set; }

    public string? DeterminedLevel { get; set; }
    public int? OverallScore { get; set; }

    public bool HasCompletedSurvey { get; set; }
    public bool HasSkippedSurvey { get; set; }
    public bool HasCompletedVoiceAssessment { get; set; }


    public bool IsVoiceAssessmentPending { get; set; }
    public DateTime? VoiceAssessmentPendingAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? SurveyCompletedAt { get; set; }
    public DateTime? VoiceAssessmentCompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

 
    public VoiceLearningRoadmapDto? Roadmap { get; set; }
    public List<RecommendedCourseDto>? RecommendedCourses { get; set; }



    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}


public class AcceptVoiceAssessmentRequestDto
{
    public Guid UserGoalId { get; set; }
    public bool IsAccepted { get; set; }

}