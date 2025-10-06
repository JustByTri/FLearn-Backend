using BLL.IServices.UserGoal;
using Common.DTO.Assement;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services.UserGoal
{
    public class UserGoalService : IUserGoalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserGoalService> _logger;

        public UserGoalService(IUnitOfWork unitOfWork, ILogger<UserGoalService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> NeedsSurveyAsync(Guid userId, Guid languageId)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByUserAndLanguageAsync(userId, languageId);

                // Chưa có record hoặc chưa hoàn thành survey và chưa skip
                return userGoal == null || (!userGoal.HasCompletedSurvey && !userGoal.HasSkippedSurvey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking survey need for user {UserId}, language {LanguageId}", userId, languageId);
                return true; // Safe default - ask for survey
            }
        }

        public async Task<bool> HasSkippedSurveyAsync(Guid userId, Guid languageId)
        {
            try
            {
                return await _unitOfWork.UserGoals.HasUserSkippedSurveyForLanguageAsync(userId, languageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking survey skip status for user {UserId}, language {LanguageId}", userId, languageId);
                return false;
            }
        }

        public async Task MarkSurveyCompletedAsync(Guid userId, Guid languageId, int? goalId)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByUserAndLanguageAsync(userId, languageId);

                if (userGoal == null)
                {
                    userGoal = new DAL.Models.UserGoal
                    {
                        UserGoalID = Guid.NewGuid(),
                        UserID = userId,
                        LanguageID = languageId,
                        GoalId = goalId,
                        HasCompletedSurvey = true,
                        SurveyCompletedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.UserGoals.CreateAsync(userGoal);
                }
                else
                {
                    userGoal.GoalId = goalId;
                    userGoal.HasCompletedSurvey = true;
                    userGoal.HasSkippedSurvey = false;
                    userGoal.SurveyCompletedAt = DateTime.UtcNow;
                    userGoal.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.UserGoals.UpdateAsync(userGoal);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Marked survey completed for user {UserId}, language {LanguageId}, goal {GoalId}",
                    userId, languageId, goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking survey completed for user {UserId}", userId);
                throw;
            }
        }

        public async Task SkipSurveyAsync(Guid userId, Guid languageId)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByUserAndLanguageAsync(userId, languageId);

                if (userGoal == null)
                {
                    userGoal = new DAL.Models.UserGoal
                    {
                        UserGoalID = Guid.NewGuid(),
                        UserID = userId,
                        LanguageID = languageId,
                        HasSkippedSurvey = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.UserGoals.CreateAsync(userGoal);
                }
                else
                {
                    userGoal.HasSkippedSurvey = true;
                    userGoal.HasCompletedSurvey = false;
                    userGoal.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.UserGoals.UpdateAsync(userGoal);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Marked survey skipped for user {UserId}, language {LanguageId}", userId, languageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error skipping survey for user {UserId}", userId);
                throw;
            }
        }

        public async Task SaveVoiceAssessmentResultAsync(Guid userId, VoiceAssessmentResultDto assessmentResult)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByUserAndLanguageAsync(userId, assessmentResult.LaguageID);

                if (userGoal == null)
                {
                    userGoal = new DAL.Models.UserGoal
                    {
                        UserGoalID = Guid.NewGuid(),
                        UserID = userId,
                        LanguageID = assessmentResult.LaguageID,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserGoals.CreateAsync(userGoal);
                }

                // Cập nhật thông tin voice assessment
                userGoal.DeterminedLevel = assessmentResult.DeterminedLevel;
                userGoal.OverallScore = assessmentResult.OverallScore;
                userGoal.HasCompletedVoiceAssessment = true;
                userGoal.VoiceAssessmentCompletedAt = DateTime.UtcNow;
                userGoal.UpdatedAt = DateTime.UtcNow;

                // Serialize roadmap and courses
                if (assessmentResult.Roadmap != null)
                {
                    userGoal.RoadmapData = JsonSerializer.Serialize(assessmentResult.Roadmap);
                }

                if (assessmentResult.RecommendedCourses?.Any() == true)
                {
                    userGoal.RecommendedCoursesData = JsonSerializer.Serialize(assessmentResult.RecommendedCourses);
                }

                await _unitOfWork.UserGoals.UpdateAsync(userGoal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Saved voice assessment result for user {UserId}, level {Level}, score {Score}",
                    userId, assessmentResult.DeterminedLevel, assessmentResult.OverallScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving voice assessment result for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserGoalDto?> GetUserGoalAsync(Guid userId, Guid languageId)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByUserAndLanguageAsync(userId, languageId);
                if (userGoal == null) return null;

                return MapToDto(userGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user goal for user {UserId}, language {LanguageId}", userId, languageId);
                throw;
            }
        }

        public async Task<List<UserGoalDto>> GetUserGoalsAsync(Guid userId)
        {
            try
            {
                var userGoals = await _unitOfWork.UserGoals.GetByUserIdAsync(userId);
                return userGoals.Select(ug => MapToDto(ug)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user goals for user {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateRoadmapAsync(Guid userGoalId, VoiceLearningRoadmapDto roadmap)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByIdAsync(userGoalId);
                if (userGoal == null)
                    throw new ArgumentException("UserGoal not found");

                userGoal.RoadmapData = JsonSerializer.Serialize(roadmap);
                userGoal.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.UserGoals.UpdateAsync(userGoal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Updated roadmap for UserGoal {UserGoalId}", userGoalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roadmap for UserGoal {UserGoalId}", userGoalId);
                throw;
            }
        }

        // ✅ Voice Assessment Pending Methods
        public async Task<UserGoalDto> CreatePendingVoiceAssessmentResultAsync(Guid userId, VoiceAssessmentResultDto assessmentResult)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByUserAndLanguageAsync(userId, assessmentResult.LaguageID);

                if (userGoal == null)
                {
                    userGoal = new DAL.Models.UserGoal
                    {
                        UserGoalID = Guid.NewGuid(),
                        UserID = userId,
                        LanguageID = assessmentResult.LaguageID,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserGoals.CreateAsync(userGoal);
                }

                // Lưu vào pending fields
                userGoal.IsVoiceAssessmentPending = true;
                userGoal.VoiceAssessmentPendingAt = DateTime.UtcNow;
                userGoal.PendingDeterminedLevel = assessmentResult.DeterminedLevel;
                userGoal.PendingOverallScore = assessmentResult.OverallScore;

                // Serialize pending roadmap and courses
                if (assessmentResult.Roadmap != null)
                {
                    userGoal.PendingRoadmapData = JsonSerializer.Serialize(assessmentResult.Roadmap);
                }

                if (assessmentResult.RecommendedCourses?.Any() == true)
                {
                    userGoal.PendingRecommendedCoursesData = JsonSerializer.Serialize(assessmentResult.RecommendedCourses);
                }

                userGoal.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.UserGoals.UpdateAsync(userGoal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Created pending voice assessment result for user {UserId}, userGoalId {UserGoalId}",
                    userId, userGoal.UserGoalID);

                return MapToDto(userGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pending voice assessment result for user {UserId}", userId);
                throw;
            }
        }

        public async Task AcceptVoiceAssessmentResultAsync(Guid userGoalId, Guid userId)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByIdAsync(userGoalId);

                if (userGoal == null || userGoal.UserID != userId)
                    throw new ArgumentException("UserGoal không tồn tại hoặc không thuộc về user này");

                if (!userGoal.IsVoiceAssessmentPending)
                    throw new InvalidOperationException("Không có voice assessment result nào đang pending");

                // Move pending data to actual fields
                userGoal.DeterminedLevel = userGoal.PendingDeterminedLevel;
                userGoal.OverallScore = userGoal.PendingOverallScore;
                userGoal.RoadmapData = userGoal.PendingRoadmapData;
                userGoal.RecommendedCoursesData = userGoal.PendingRecommendedCoursesData;

                // Update status
                userGoal.HasCompletedVoiceAssessment = true;
                userGoal.IsVoiceAssessmentPending = false;
                userGoal.VoiceAssessmentCompletedAt = DateTime.UtcNow;
                userGoal.UpdatedAt = DateTime.UtcNow;

                // Clear pending fields
                ClearVoiceAssessmentPendingFields(userGoal);

                await _unitOfWork.UserGoals.UpdateAsync(userGoal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Accepted voice assessment result for UserGoal {UserGoalId}", userGoalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting voice assessment result for UserGoal {UserGoalId}", userGoalId);
                throw;
            }
        }

        public async Task RejectVoiceAssessmentResultAsync(Guid userGoalId, Guid userId)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByIdAsync(userGoalId);

                if (userGoal == null || userGoal.UserID != userId)
                    throw new ArgumentException("UserGoal không tồn tại hoặc không thuộc về user này");

                if (!userGoal.IsVoiceAssessmentPending)
                    throw new InvalidOperationException("Không có voice assessment result nào đang pending");

                // Clear pending status and data
                userGoal.IsVoiceAssessmentPending = false;
                userGoal.VoiceAssessmentPendingAt = null;
                userGoal.UpdatedAt = DateTime.UtcNow;

                // Clear pending fields
                ClearVoiceAssessmentPendingFields(userGoal);

                await _unitOfWork.UserGoals.UpdateAsync(userGoal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Rejected voice assessment result for UserGoal {UserGoalId}", userGoalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting voice assessment result for UserGoal {UserGoalId}", userGoalId);
                throw;
            }
        }

        // ✅ Survey Pending Methods (chưa implement - placeholder)
        public async Task<UserGoalDto> CreatePendingSurveyResultAsync(Guid userId, Guid languageId, int? goalId)
        {
            // TODO: Implement survey pending logic nếu cần
            throw new NotImplementedException("Survey pending chưa được implement");
        }

        public async Task AcceptSurveyResultAsync(Guid userGoalId, Guid userId)
        {
            // TODO: Implement survey accept logic nếu cần
            throw new NotImplementedException("Survey accept chưa được implement");
        }

        public async Task RejectSurveyResultAsync(Guid userGoalId, Guid userId)
        {
            // TODO: Implement survey reject logic nếu cần
            throw new NotImplementedException("Survey reject chưa được implement");
        }

        public async Task<UserGoalDto?> GetUserGoalByIdAsync(Guid userGoalId, Guid userId)
        {
            try
            {
                var userGoal = await _unitOfWork.UserGoals.GetByIdAsync(userGoalId);

                if (userGoal == null || userGoal.UserID != userId)
                    return null;

                return MapToDto(userGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UserGoal {UserGoalId} for user {UserId}", userGoalId, userId);
                throw;
            }
        }

        // ✅ Helper Methods
        private void ClearVoiceAssessmentPendingFields(DAL.Models.UserGoal userGoal)
        {
            userGoal.PendingDeterminedLevel = null;
            userGoal.PendingOverallScore = null;
            userGoal.PendingRoadmapData = null;
            userGoal.PendingRecommendedCoursesData = null;
        }

        private UserGoalDto MapToDto(DAL.Models.UserGoal userGoal)
        {
            var dto = new UserGoalDto
            {
                UserGoalID = userGoal.UserGoalID,
                UserID = userGoal.UserID,
                LanguageID = userGoal.LanguageID,
                LanguageName = userGoal.Language?.LanguageName ?? "",
                GoalId = userGoal.GoalId,
                GoalName = userGoal.Goal?.Name,

                // Show pending data if pending, otherwise show actual data
                DeterminedLevel = userGoal.IsVoiceAssessmentPending ? userGoal.PendingDeterminedLevel : userGoal.DeterminedLevel,
                OverallScore = userGoal.IsVoiceAssessmentPending ? userGoal.PendingOverallScore : userGoal.OverallScore,

                HasCompletedSurvey = userGoal.HasCompletedSurvey,
                HasSkippedSurvey = userGoal.HasSkippedSurvey,
                HasCompletedVoiceAssessment = userGoal.HasCompletedVoiceAssessment,

                // NEW: Pending status
                IsVoiceAssessmentPending = userGoal.IsVoiceAssessmentPending,
                VoiceAssessmentPendingAt = userGoal.VoiceAssessmentPendingAt,

                CreatedAt = userGoal.CreatedAt,
                SurveyCompletedAt = userGoal.SurveyCompletedAt,
                VoiceAssessmentCompletedAt = userGoal.VoiceAssessmentCompletedAt,
                UpdatedAt = userGoal.UpdatedAt,

                Notes = userGoal.Notes,
                IsActive = userGoal.IsActive
            };

            // Deserialize roadmap and courses (use pending if pending, otherwise actual)
            var roadmapData = userGoal.IsVoiceAssessmentPending ? userGoal.PendingRoadmapData : userGoal.RoadmapData;
            var coursesData = userGoal.IsVoiceAssessmentPending ? userGoal.PendingRecommendedCoursesData : userGoal.RecommendedCoursesData;

            if (!string.IsNullOrEmpty(roadmapData))
            {
                try
                {
                    dto.Roadmap = JsonSerializer.Deserialize<VoiceLearningRoadmapDto>(roadmapData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize roadmap for UserGoal {UserGoalId}", userGoal.UserGoalID);
                }
            }

            if (!string.IsNullOrEmpty(coursesData))
            {
                try
                {
                    dto.RecommendedCourses = JsonSerializer.Deserialize<List<RecommendedCourseDto>>(coursesData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize courses for UserGoal {UserGoalId}", userGoal.UserGoalID);
                }
            }

            return dto;
        }
        public async Task<DAL.Models.UserGoal?> GetUserGoalByLanguageAsync(Guid userId, Guid languageId)
        {
            try
            {
                var userGoals = await _unitOfWork.UserGoals.GetAllAsync();

                return userGoals.FirstOrDefault(ug =>
                    ug.UserID == userId &&
                    ug.LanguageID == languageId &&
                    ug.VoiceAssessmentPendingAt.HasValue
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UserGoal by language for user {UserId}", userId);
                return null;
            }
        }
        public async Task<DAL.Models.UserGoal> CreateSkippedVoiceAssessmentAsync(
         Guid userId,
         Guid languageId,
         string languageName,
         int? goalId = null)
        {
            try
            {
                // Xóa UserGoal cũ nếu có
                var existingUserGoal = await GetUserGoalByLanguageAsync(userId, languageId);
                if (existingUserGoal != null)
                {
                    // ✅ Dùng RemoveAsync thay vì DeleteAsync
                    await _unitOfWork.UserGoals.RemoveAsync(existingUserGoal);
                }

                var userGoal = new DAL.Models.UserGoal
                {
                    UserGoalID = Guid.NewGuid(),
                    UserID = userId,
                    GoalId = goalId,
                    LanguageID = languageId,
                    DeterminedLevel = "Not Assessed",
                    UpdatedAt = DateTime.UtcNow,
                    IsVoiceAssessmentPending = false,
                    VoiceAssessmentPendingAt = null,
                    RoadmapData = GenerateBasicRoadmap(languageName, "Not Assessed"),
                    RecommendedCoursesData = "Bạn có thể bắt đầu với các khóa học cơ bản",
                    CreatedAt = DateTime.UtcNow
                };

                // ✅ Dùng CreateAsync thay vì AddAsync
                await _unitOfWork.UserGoals.CreateAsync(userGoal);

                // ❌ Không cần SaveChanges vì CreateAsync đã save rồi
                // await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created skipped voice assessment UserGoal for user {UserId}", userId);

                return userGoal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating skipped voice assessment");
                throw;
            }
        }

        private string GenerateBasicRoadmap(string languageName, string level)
        {
            return $@"# Lộ trình học {languageName}

## Bạn đã bỏ qua đánh giá giọng nói

Bạn có thể bắt đầu học với các khóa học cơ bản phù hợp với trình độ của mình.

### Gợi ý:
1. Bắt đầu với khóa học Beginner
2. Luyện tập thường xuyên
3. Có thể làm voice assessment sau để đánh giá chính xác hơn";
        }
    }
}

