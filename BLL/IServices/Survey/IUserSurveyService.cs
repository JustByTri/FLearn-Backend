using Common.DTO.Learner;

namespace BLL.IServices.Survey
{
    public interface IUserSurveyService
    {
        Task<UserSurveyResponseDto> CreateSurveyAsync(Guid userId, UserSurveyDto surveyDto);
        Task<UserSurveyResponseDto?> GetUserSurveyAsync(Guid userId);
        Task<bool> HasUserCompletedSurveyAsync(Guid userId);
        Task<AiCourseRecommendationDto> GenerateRecommendationsAsync(Guid userId);

     
        Task<List<string>> GetLearningGoalOptionsAsync();
        Task<List<string>> GetCurrentLevelOptionsAsync();
        Task<List<string>> GetLearningStyleOptionsAsync();
        Task<List<string>> GetPrioritySkillsOptionsAsync();
        Task<List<string>> GetTargetTimelineOptionsAsync();

  
        Task<List<string>> GetSpeakingChallengesOptionsAsync();
        Task<List<string>> GetPreferredAccentOptionsAsync();
    }
}

