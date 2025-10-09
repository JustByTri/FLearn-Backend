using Common.DTO.Assement;
using Common.DTO.Learner;

namespace BLL.IServices.Assessment
{
    public interface IVoiceAssessmentService
    {
        Task<VoiceAssessmentDto> StartVoiceAssessmentAsync(Guid userId, Guid languageId, int? goalId = null);
        Task<VoiceAssessmentQuestion> GetCurrentQuestionAsync(Guid assessmentId);
        Task SubmitVoiceResponseAsync(Guid assessmentId, VoiceAssessmentResponseDto response);
        Task<BatchVoiceEvaluationResult> CompleteVoiceAssessmentAsync(Guid assessmentId);
        Task<VoiceAssessmentResultDto?> GetVoiceAssessmentResultAsync(Guid userId, Guid languageId);
        Task<bool> HasCompletedVoiceAssessmentAsync(Guid userId, Guid languageId);

    
        Task<List<VoiceAssessmentDto>> GetActiveAssessmentsDebugAsync();

    
        Task<Guid?> FindAssessmentIdAsync(Guid userId, Guid languageId);
        Task<VoiceAssessmentDto?> RestoreAssessmentFromIdAsync(Guid assessmentId);
        Task<bool> ValidateAssessmentIdAsync(Guid assessmentId, Guid userId);

        /// <summary>
        /// Xóa kết quả assessment cũ khi đổi ngôn ngữ
        /// </summary>
        Task ClearAssessmentResultAsync(Guid userId, Guid languageId);
        Task SaveRecommendedCoursesAsync(Guid userId, Guid languageId, List<CourseRecommendationDto> courses);

    }
}

