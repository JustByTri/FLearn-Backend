using Common.DTO.Learner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Course
{

    public interface ICourseRecommendationService
    {
        /// <summary>
        /// Lấy danh sách khóa học được gợi ý dựa trên ngôn ngữ và trình độ
        /// </summary>
        Task<List<CourseRecommendationDto>> GetRecommendedCoursesAsync(
            Guid languageId,
            string determinedLevel,
            int? goalId = null);

        /// <summary>
        /// Kiểm tra xem có khóa học nào cho ngôn ngữ và level không
        /// </summary>
        Task<bool> HasCoursesForLevelAsync(Guid languageId, string level);

        /// <summary>
        /// Lấy tất cả levels có sẵn cho một ngôn ngữ
        /// </summary>
        Task<List<string>> GetAvailableLevelsForLanguageAsync(Guid languageId);
    }
}
