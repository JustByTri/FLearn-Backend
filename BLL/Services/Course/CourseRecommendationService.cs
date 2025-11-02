
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using DAL.UnitOfWork;
//using DAL.Models;
//using DAL.Type;
//using Common.DTO.Learner;
//using BLL.Services;
//using Microsoft.Extensions.Logging;
//using BLL.IServices.Course;

//namespace BLL.Services.Implementation
//{
//    public class CourseRecommendationService : ICourseRecommendationService
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly ILogger<CourseRecommendationService> _logger;

//        public CourseRecommendationService(
//            IUnitOfWork unitOfWork,
//            ILogger<CourseRecommendationService> logger)
//        {
//            _unitOfWork = unitOfWork;
//            _logger = logger;
//        }

//        public async Task<List<CourseRecommendationDto>> GetRecommendedCoursesAsync(
//            Guid languageId,
//            string determinedLevel,
//            int? goalId = null)
//        {
//            try
//            {
//                _logger.LogInformation("🔍 Searching courses: LanguageId={LanguageId}, Level={Level}, GoalId={GoalId}",
//                    languageId, determinedLevel, goalId);

//                // Lấy tất cả courses
//                var allCourses = await _unitOfWork.Courses.GetAllAsync();

//                // ✅ Filter theo language và status = Published (chỉ lấy khóa đã publish)
//                var coursesForLanguage = allCourses
//                    .Where(c => c.LanguageId == languageId && c.Status == CourseStatus.Published)
//                    .ToList();

//                _logger.LogInformation("📚 Found {Count} published courses for this language", coursesForLanguage.Count);

//                if (!coursesForLanguage.Any())
//                {
//                    _logger.LogWarning("⚠️ No published courses found for LanguageId={LanguageId}", languageId);
//                    return new List<CourseRecommendationDto>();
//                }

//                // Map string level sang enum
//                var targetLevel = MapAssessmentLevelToEnum(determinedLevel);

//                _logger.LogInformation("🎯 Target level: {Level}", targetLevel);

//                // Filter theo level và tính match score
//                var matchedCourses = coursesForLanguage
//                    .Select(c => new
//                    {
//                        Course = c,
//                        MatchScore = CalculateMatchScore(c, targetLevel, goalId)
//                    })
//                    .Where(x => x.MatchScore > 0)
//                    .OrderByDescending(x => x.MatchScore)
//                    .ThenBy(x => x.Course.Level)
//                    .Take(5)
//                    .ToList();

//                // Nếu không có khóa phù hợp, lấy 3 khóa gần nhất
//                if (!matchedCourses.Any())
//                {
//                    _logger.LogWarning("⚠️ No exact match found, suggesting closest courses");

//                    matchedCourses = coursesForLanguage
//                        .OrderBy(c => c.Level)
//                        .Take(3)
//                        .Select(c => new
//                        {
//                            Course = c,
//                            MatchScore = 50m
//                        })
//                        .ToList();
//                }

//                _logger.LogInformation("Searching courses: LanguageId={LanguageId}, Level={Level}, GoalId={GoalId}",
//    languageId, determinedLevel, goalId);

//                return MapToCourseDtos(
//                    matchedCourses.Select(x => x.Course).ToList(),
//                    matchedCourses.ToDictionary(x => x.Course.CourseID, x => x.MatchScore),
//                    determinedLevel);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Error finding recommended courses");
//                return new List<CourseRecommendationDto>();
//            }
//        }

//        public async Task<bool> HasCoursesForLevelAsync(Guid languageId, string level)
//        {
//            try
//            {
//                var allCourses = await _unitOfWork.Courses.GetAllAsync();
//                var targetLevel = MapAssessmentLevelToEnum(level);

//                // ✅ Chỉ check courses đã Published
//                var hasMatch = allCourses.Any(c =>
//                    c.LanguageId == languageId &&
//                    c.Status == CourseStatus.Published &&
//                    c.Level == targetLevel);

//                _logger.LogInformation("Course availability check: LanguageId={LanguageId}, Level={Level}, HasCourses={Result}",
//                    languageId, level, hasMatch);

//                return hasMatch;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Error checking course availability");
//                return false;
//            }
//        }

//        public async Task<List<string>> GetAvailableLevelsForLanguageAsync(Guid languageId)
//        {
//            try
//            {
//                var allCourses = await _unitOfWork.Courses.GetAllAsync();

//                // ✅ Chỉ lấy levels từ courses đã Published
//                var availableLevels = allCourses
//                    .Where(c => c.LanguageId == languageId && c.Status == CourseStatus.Published)
//                    .Select(c => c.Level.ToString())
//                    .Distinct()
//                    .OrderBy(l => l)
//                    .ToList();

//                _logger.LogInformation("Available levels for LanguageId={LanguageId}: {Levels}",
//                    languageId, string.Join(", ", availableLevels));

//                return availableLevels;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Error getting available levels");
//                return new List<string>();
//            }
//        }

//        #region Private Helpers

//        private decimal CalculateMatchScore(DAL.Models.Course course, LevelType targetLevel, int? goalId)
//        {
//            decimal score = 0;

//            // Level match (70 points)
//            if (course.Level == targetLevel)
//            {
//                score += 70;  // Perfect match
//            }
//            else
//            {
//                int levelDiff = Math.Abs((int)course.Level - (int)targetLevel);

//                if (levelDiff == 1)
//                {
//                    score += 50;  // Adjacent level
//                }
//                else if (levelDiff == 2)
//                {
//                    score += 20;  // 2 levels away
//                }
//            }

//            // Goal match (20 points)
//            if (goalId.HasValue)
//            {
//                score += 10;
//            }

//            // Quality bonus (10 points)
//            score += 10;

//            return Math.Min(100, Math.Max(0, score));
//        }

//        private string GenerateMatchReason(DAL.Models.Course course, LevelType targetLevel, decimal matchScore)
//        {
//            var reasons = new List<string>();

//            if (course.Level == targetLevel)
//            {
//                reasons.Add($"Khóa học phù hợp chính xác với trình độ {targetLevel} của bạn");
//            }
//            else
//            {
//                int levelDiff = (int)course.Level - (int)targetLevel;

//                if (levelDiff == 1)
//                {
//                    reasons.Add($"Khóa học ở mức độ tiếp theo, giúp bạn phát triển từ {targetLevel}");
//                }
//                else if (levelDiff == -1)
//                {
//                    reasons.Add("Khóa học giúp củng cố nền tảng trước khi nâng cao");
//                }
//                else
//                {
//                    reasons.Add("Khóa học phù hợp với mục tiêu học tập của bạn");
//                }
//            }

//            if (matchScore >= 80)
//            {
//                reasons.Add("Tỷ lệ phù hợp cao");
//            }
//            else if (matchScore >= 60)
//            {
//                reasons.Add("Tỷ lệ phù hợp tốt");
//            }

//            return string.Join(". ", reasons);
//        }

//        private LevelType MapAssessmentLevelToEnum(string assessmentLevel)
//        {
//            if (string.IsNullOrEmpty(assessmentLevel))
//            {
//                return LevelType.Beginner;
//            }

//            var normalized = assessmentLevel.ToLower().Trim();

//            return normalized switch
//            {
//                "beginner" or "a1" or "a2" or "elementary" or "basic" => LevelType.Beginner,
//                "intermediate" or "b1" or "b2" or "pre-intermediate" or "upper-intermediate" => LevelType.Intermediate,
//                "advanced" or "c1" or "c2" or "proficient" or "fluent" or "native" => LevelType.Advanced,
//                _ => LevelType.Beginner
//            };
//        }

//        private List<CourseRecommendationDto> MapToCourseDtos(
//            List<DAL.Models.Course> courses,
//            Dictionary<Guid, decimal> matchScores,
//            string determinedLevel)
//        {
//            if (courses == null || !courses.Any())
//            {
//                return new List<CourseRecommendationDto>();
//            }

//            var targetLevel = MapAssessmentLevelToEnum(determinedLevel);
//            var result = new List<CourseRecommendationDto>();

//            foreach (var course in courses)
//            {
//                var matchScore = matchScores.ContainsKey(course.CourseID)
//                    ? matchScores[course.CourseID]
//                    : 50m;

//                var dto = new CourseRecommendationDto
//                {
//                    CourseID = course.CourseID,
//                    CourseName = course.Title ?? "Untitled Course",
//                    CourseDescription = course.Description ?? "No description available",
//                    Level = course.Level.ToString(),
//                    MatchScore = matchScore,
//                    MatchReason = GenerateMatchReason(course, targetLevel, matchScore),
//                    EstimatedDuration = course.NumLessons * 2,

//                };

//                result.Add(dto);
//            }

//            return result;
//        }

//        private List<string> GenerateSkillsList(LevelType level)
//        {
//            return level switch
//            {
//                LevelType.Beginner => new List<string>
//                {
//                    "Phát âm cơ bản",
//                    "Từ vựng thông dụng",
//                    "Ngữ pháp nền tảng",
//                    "Giao tiếp đơn giản"
//                },
//                LevelType.Intermediate => new List<string>
//                {
//                    "Giao tiếp tự tin",
//                    "Ngữ pháp nâng cao",
//                    "Viết văn bản",
//                    "Nghe hiểu tốt"
//                },
//                LevelType.Advanced => new List<string>
//                {
//                    "Giao tiếp trôi chảy",
//                    "Hiểu văn hóa sâu sắc",
//                    "Viết chuyên nghiệp",
//                    "Thảo luận phức tạp"
//                },
//                _ => new List<string> { "Kỹ năng tổng hợp" }
//            };
//        }

//        #endregion
//    }
//}
