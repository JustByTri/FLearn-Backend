using Common.DTO.Assement;
using DAL.Models;


namespace BLL.IServices.UserGoal
{
    public interface IUserGoalService
    {
        // Check survey requirements
        Task<bool> NeedsSurveyAsync(Guid userId, Guid languageId);
        Task<bool> HasSkippedSurveyAsync(Guid userId, Guid languageId);

        Task<UserGoalDto> CreatePendingSurveyResultAsync(Guid userId, Guid languageId, int? goalId);
        Task AcceptSurveyResultAsync(Guid userGoalId, Guid userId);
        Task RejectSurveyResultAsync(Guid userGoalId, Guid userId);
        Task SkipSurveyAsync(Guid userId, Guid languageId);

        // Voice Assessment actions
        Task SaveVoiceAssessmentResultAsync(Guid userId, VoiceAssessmentResultDto assessmentResult);

        // Get user goals
        Task<UserGoalDto?> GetUserGoalAsync(Guid userId, Guid languageId);
        Task<UserGoalDto?> GetUserGoalByIdAsync(Guid userGoalId, Guid userId);
        Task<List<UserGoalDto>> GetUserGoalsAsync(Guid userId);

        // Update roadmap
        Task<UserGoalDto> CreatePendingVoiceAssessmentResultAsync(Guid userId, VoiceAssessmentResultDto assessmentResult);
        Task AcceptVoiceAssessmentResultAsync(Guid userGoalId, Guid userId);
        Task RejectVoiceAssessmentResultAsync(Guid userGoalId, Guid userId);
        Task UpdateRoadmapAsync(Guid userGoalId, VoiceLearningRoadmapDto roadmap);
        Task<DAL.Models.UserGoal?> GetUserGoalByLanguageAsync(Guid userId, Guid languageId);
        Task<DAL.Models.UserGoal> CreateSkippedVoiceAssessmentAsync(Guid userId, Guid languageId, string languageName, int? goalId = null);

    }
}
