using Common.DTO.Assement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Redis
{
    public interface IRedisService
    {
        // Voice Assessment operations
        Task<VoiceAssessmentDto?> GetVoiceAssessmentAsync(Guid assessmentId);
        Task SetVoiceAssessmentAsync(VoiceAssessmentDto assessment);
        Task<bool> DeleteVoiceAssessmentAsync(Guid assessmentId);
        Task<List<VoiceAssessmentDto>> GetActiveVoiceAssessmentsAsync();
        Task<List<VoiceAssessmentDto>> GetUserAssessmentsAsync(Guid userId, Guid? languageId = null);

        // Voice Assessment Results
        /// <summary>
        /// Lấy kết quả assessment đã hoàn thành từ Redis
        /// (Keyed bằng LearnerLanguageId)
        /// </summary>
        Task<VoiceAssessmentResultDto?> GetVoiceAssessmentResultAsync(Guid learnerLanguageId);

        /// <summary>
        /// Lưu kết quả assessment đã hoàn thành vào Redis
        /// (Keyed bằng LearnerLanguageId)
        /// </summary>
        Task SetVoiceAssessmentResultAsync(Guid learnerLanguageId, VoiceAssessmentResultDto result);

        /// <summary>
        /// Xóa kết quả assessment đã hoàn thành khỏi Redis
        /// (Keyed bằng LearnerLanguageId)
        /// </summary>
        Task DeleteVoiceAssessmentResultAsync(Guid learnerLanguageId);

        // ----- Hàm Debug (giữ nguyên) -----
        Task<int> ClearAllAssessmentsAsync();

        /// <summary>
        /// Delete generic key
        /// </summary>
        Task DeleteAsync(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T?> GetAsync<T>(string key) where T : class;
    }
}
