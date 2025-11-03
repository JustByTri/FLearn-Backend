using Common.DTO.Assement;
using Common.DTO.Learner;

namespace BLL.IServices.Assessment
{
    public interface IVoiceAssessmentService
    {
        /// <summary>
        /// Bắt đầu bài đánh giá giọng nói dựa trên Ngôn ngữ và Khung chương trình.
        /// </summary>
        Task<VoiceAssessmentDto> StartProgramAssessmentAsync(Guid userId, Guid languageId, Guid programId);

        /// <summary>
        /// Lấy câu hỏi hiện tại từ bài đánh giá trong Redis.
        /// </summary>
        Task<VoiceAssessmentQuestion> GetCurrentQuestionAsync(Guid assessmentId);

        /// <summary>
        /// Nộp file audio hoặc bỏ qua một câu hỏi.
        /// </summary>
        Task SubmitVoiceResponseAsync(Guid assessmentId, VoiceAssessmentResponseDto response);

        /// <summary>
        /// Hoàn thành bài đánh giá: Đánh giá bằng AI và Gợi ý khóa học.
        /// </summary>
        /// <returns>Kết quả đầy đủ (bao gồm các khóa học gợi ý).</returns>
        Task<VoiceAssessmentResultDto> CompleteProgramAssessmentAsync(Guid assessmentId);

        /// <summary>
        /// Lấy kết quả (đã lưu tạm) từ Redis bằng LearnerLanguageId.
        /// </summary>
        Task<VoiceAssessmentResultDto?> GetAssessmentResultAsync(Guid learnerLanguageId);

        /// <summary>
        /// Chấp nhận kết quả: Lưu ProficiencyLevel vào LearnerLanguage.
        /// </summary>
        Task AcceptAssessmentAsync(Guid learnerLanguageId);

        /// <summary>
        /// Từ chối/Hủy kết quả: Xóa kết quả tạm thời khỏi Redis.
        /// </summary>
        Task RejectAssessmentAsync(Guid learnerLanguageId);

        /// <summary>
        /// Xác thực xem assessmentId có thuộc về userId hay không.
        /// </summary>
        Task<bool> ValidateAssessmentIdAsync(Guid assessmentId, Guid userId);

        /// <summary>
        /// Khôi phục bài đánh giá từ Redis (dùng nội bộ).
        /// </summary>
        Task<VoiceAssessmentDto?> RestoreAssessmentFromIdAsync(Guid assessmentId);
    }
}