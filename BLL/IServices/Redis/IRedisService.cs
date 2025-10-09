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
        Task<VoiceAssessmentResultDto?> GetVoiceAssessmentResultAsync(Guid userId, Guid languageId);
        Task SetVoiceAssessmentResultAsync(Guid userId, Guid languageId, VoiceAssessmentResultDto result);

        // Utility
        Task<int> ClearAllAssessmentsAsync();
        /// <summary>
        /// Xóa kết quả voice assessment result
        /// </summary>
        Task DeleteVoiceAssessmentResultAsync(Guid userId, Guid languageId);

        /// <summary>
        /// Delete generic key
        /// </summary>
        Task DeleteAsync(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    }
}
