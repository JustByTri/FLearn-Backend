using BLL.IServices.AI;
using BLL.IServices.Survey;
using Common.DTO.Learner;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BLL.Services.Survey
{
    public class UserSurveyService : IUserSurveyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<UserSurveyService> _logger;

        public UserSurveyService(
            IUnitOfWork unitOfWork,
            IGeminiService geminiService,
            ILogger<UserSurveyService> logger)
        {
            _unitOfWork = unitOfWork;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<UserSurveyResponseDto> CreateSurveyAsync(Guid userId, UserSurveyDto surveyDto)
        {
            try
            {
                // Check if user already has a survey
                var existingSurvey = await _unitOfWork.UserSurveys.GetByUserIdAsync(userId);
                if (existingSurvey != null)
                {
                    throw new InvalidOperationException("Bạn đã hoàn thành khảo sát trước đó. Bạn có thể cập nhật thông tin trong phần cài đặt.");
                }


                var language = await _unitOfWork.Languages.GetByIdAsync(surveyDto.PreferredLanguageID);
                if (language == null)
                {
                    throw new ArgumentException("Ngôn ngữ được chọn không tồn tại");
                }


                var supportedLanguages = new[] { "EN", "ZH", "JP" }; // English, Chinese, Japanese
                if (!supportedLanguages.Contains(language.LanguageCode))
                {
                    throw new ArgumentException("Hiện tại chúng tôi chỉ hỗ trợ học speaking tiếng Anh, tiếng Trung và tiếng Nhật");
                }


                var survey = new UserSurvey
                {
                    SurveyID = Guid.NewGuid(),
                    UserID = userId,
                    CurrentLevel = surveyDto.CurrentLevel,
                    PreferredLanguageID = surveyDto.PreferredLanguageID,

                    LearningReason = surveyDto.LearningReason,
                    PreviousExperience = surveyDto.PreviousExperience,
                    PreferredLearningStyle = surveyDto.PreferredLearningStyle,
                    InterestedTopics = surveyDto.InterestedTopics,
                    PrioritySkills = string.IsNullOrEmpty(surveyDto.PrioritySkills) ? "Speaking" : surveyDto.PrioritySkills,
                    TargetTimeline = surveyDto.TargetTimeline,


                    SpeakingChallenges = surveyDto.SpeakingChallenges ?? string.Empty,
                    ConfidenceLevel = surveyDto.ConfidenceLevel,
                    PreferredAccent = surveyDto.PreferredAccent ?? "No Preference",

                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };


                await _unitOfWork.UserSurveys.CreateAsync(survey);


                await EnsureUserLanguageTrackingAsync(userId, surveyDto.PreferredLanguageID);


                try
                {
                    var recommendations = await GenerateRecommendationsAsync(userId);
                    survey.AiRecommendations = JsonSerializer.Serialize(recommendations);
                    await _unitOfWork.UserSurveys.UpdateAsync(survey);

                    _logger.LogInformation("Successfully generated AI recommendations for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate AI recommendations for user {UserId}", userId);

                }

                await _unitOfWork.SaveChangesAsync();
                return MapToResponseDto(survey, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating survey for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserSurveyResponseDto?> GetUserSurveyAsync(Guid userId)
        {
            try
            {
                var survey = await _unitOfWork.UserSurveys.GetByUserIdAsync(userId);
                if (survey == null) return null;

                var language = await _unitOfWork.Languages.GetByIdAsync(survey.PreferredLanguageID);
                return MapToResponseDto(survey, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting survey for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> HasUserCompletedSurveyAsync(Guid userId)
        {
            try
            {
                var survey = await _unitOfWork.UserSurveys.GetByUserIdAsync(userId);
                return survey?.IsCompleted == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking survey completion for user {UserId}", userId);
                return false;
            }
        }

        public async Task<AiCourseRecommendationDto> GenerateRecommendationsAsync(Guid userId)
        {
            try
            {
                var survey = await GetUserSurveyAsync(userId);
                if (survey == null)
                {
                    throw new InvalidOperationException("Bạn cần hoàn thành khảo sát để nhận gợi ý khóa học speaking");
                }


                var courses = await _unitOfWork.Courses.GetCoursesByLanguageAsync(survey.PreferredLanguageID);


                var publishedCourses = courses.Where(c => c.Status == CourseStatus.Published).ToList();

                var courseInfos = new List<CourseInfoDto>();

                foreach (var course in publishedCourses)
                {

                    var courseTopics = await GetCourseTopicsAsync(course.CourseID);

                    var courseSkills = GetSpeakingCourseSkills(course.SkillFocus, course.Level, survey.PrioritySkills);

                    var duration = await CalculateCourseDurationAsync(course.CourseID);

                    courseInfos.Add(new CourseInfoDto
                    {
                        CourseID = course.CourseID,
                        Title = course.Title,
                        Description = course.Description,
                        Level = course.Level ?? "Beginner",
                        Language = survey.PreferredLanguageName,
                        Topics = courseTopics,
                        Skills = courseSkills,
                        Duration = duration,
                        Difficulty = MapLevelToDifficulty(course.Level)
                    });
                }


                if (!courseInfos.Any())
                {
                    _logger.LogWarning("No published speaking courses found for language {LanguageId}", survey.PreferredLanguageID);

                    return CreateFallbackRecommendations(survey);
                }

                return await _geminiService.GenerateCourseRecommendationsAsync(survey, courseInfos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations for user {UserId}", userId);
                throw;
            }
        }


        public async Task<List<string>> GetLearningGoalOptionsAsync()
        {

            return await Task.FromResult(new List<string>());
        }

        public async Task<List<string>> GetCurrentLevelOptionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "Complete Beginner", // Hoàn toàn mới bắt đầu
                "Beginner", // Biết một chút cơ bản
                "Elementary", // Có thể nói câu đơn giản
                "Pre-Intermediate", // Giao tiếp cơ bản được
                "Intermediate", // Nói khá tự tin
                "Upper-Intermediate", // Giao tiếp tốt
                "Advanced", // Nói rất lưu loát
                "Near-Native" // Gần như người bản ngữ
            });
        }

        public async Task<List<string>> GetLearningStyleOptionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "Interactive Speaking", // Tương tác trực tiếp
                "Audio-Visual Learning", // Học qua nghe và xem
                "Conversation Practice", // Luyện hội thoại
                "Role-Playing", // Đóng vai tình huống
                "Pronunciation Drilling", // Luyện phát âm chuyên sâu
                "Story Telling", // Kể chuyện
                "Debate & Discussion", // Tranh luận và thảo luận
                "Real-life Scenarios", // Tình huống thực tế
                "Self-Recording Practice" // Tự ghi âm luyện tập
            });
        }

        public async Task<List<string>> GetPrioritySkillsOptionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "Speaking", // Mặc định - always available
                "Pronunciation", // Phát âm
                "Fluency", // Lưu loát
                "Vocabulary Building", // Xây dựng từ vựng
                "Grammar in Speaking", // Ngữ pháp trong nói
                "Accent Reduction", // Giảm giọng địa phương
                "Confidence Building", // Xây dựng tự tin
                "Natural Expression", // Diễn đạt tự nhiên
                "Listening Comprehension" // Hiểu nghe (hỗ trợ speaking)
            });
        }

        public async Task<List<string>> GetTargetTimelineOptionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "1 month - Quick Basics", // 1 tháng - Cơ bản nhanh
                "3 months - Conversation Ready", // 3 tháng - Sẵn sàng hội thoại
                "6 months - Confident Speaker", // 6 tháng - Nói tự tin
                "1 year - Fluent Communication", // 1 năm - Giao tiếp lưu loát
                "2 years - Advanced Proficiency", // 2 năm - Thành thạo nâng cao
                "No rush - Steady Progress" // Không vội - Tiến bộ đều đặn
            });
        }


        public async Task<List<string>> GetSpeakingChallengesOptionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "Pronunciation Issues", // Vấn đề phát âm
                "Grammar Usage", // Sử dụng ngữ pháp
                "Limited Vocabulary", // Hạn chế từ vựng
                "Speaking Confidence", // Tự tin khi nói
                "Accent Problems", // Vấn đề giọng
                "Speaking Too Fast/Slow", // Tốc độ nói
                "Lack of Natural Flow", // Thiếu sự tự nhiên
                "Cultural Expression", // Diễn đạt văn hóa
                "Formal vs Informal Speech", // Nói trang trọng vs thân mật
                "Fear of Making Mistakes" // Sợ mắc lỗi
            });
        }

        public async Task<List<string>> GetPreferredAccentOptionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "No Preference", // Không ưu tiên
                "American English", // Tiếng Anh Mỹ
                "British English", // Tiếng Anh Anh
                "Australian English", // Tiếng Anh Úc
                "Standard Mandarin", // Tiếng Trung chuẩn
                "Taiwan Mandarin", // Tiếng Trung Đài Loan
                "Standard Japanese", // Tiếng Nhật chuẩn (Tokyo)
                "Kansai Japanese", // Tiếng Nhật Kansai
                "Native-like" // Giống người bản ngữ
            });
        }



        private async Task EnsureUserLanguageTrackingAsync(Guid userId, Guid languageId)
        {
            try
            {
                var userLanguage = await _unitOfWork.UserLearningLanguages.GetUserLearningLanguageAsync(userId, languageId);
                if (userLanguage == null)
                {
                    await _unitOfWork.UserLearningLanguages.CreateAsync(new UserLearningLanguage
                    {
                        UserLearningLanguageID = Guid.NewGuid(),
                        UserID = userId,
                        LanguageID = languageId
                    });

                    _logger.LogInformation("Added language tracking for user {UserId}, language {LanguageId}", userId, languageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring user language tracking for user {UserId}", userId);

            }
        }

        private async Task<List<string>> GetCourseTopicsAsync(Guid courseId)
        {
            try
            {

                var courseTopics = await _unitOfWork.CourseTopics.GetAllAsync();
                var topics = await _unitOfWork.Topics.GetAllAsync();

                var courseTopicNames = courseTopics
                    .Where(ct => ct.CourseID == courseId)
                    .Join(topics, ct => ct.TopicID, t => t.TopicID, (ct, t) => t.Name)
                    .ToList();

                return courseTopicNames.Any() ? courseTopicNames : GetDefaultSpeakingTopics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting topics for course {CourseId}", courseId);
                return GetDefaultSpeakingTopics();
            }
        }

        private async Task<int> CalculateCourseDurationAsync(Guid courseId)
        {
            try
            {
                var course = await _unitOfWork.Courses.GetCourseWithUnitsAsync(courseId);
                if (course?.CourseUnits?.Any() == true)
                {

                    var totalLessons = course.NumLessons > 0 ? course.NumLessons :
                                     course.CourseUnits.Count * 4;

                    return Math.Max(totalLessons * 25 / 60, 1);
                }

                return 15;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating duration for course {CourseId}", courseId);
                return 15;
            }
        }

        private List<string> GetSpeakingCourseSkills(string? skillFocus, string? level, string? prioritySkills)
        {
            var skills = new List<string> { "Speaking" };


            if (!string.IsNullOrEmpty(skillFocus))
            {
                skills.Add(skillFocus);
            }


            if (!string.IsNullOrEmpty(prioritySkills) && prioritySkills != "Speaking")
            {
                skills.Add(prioritySkills);
            }


            switch (level?.ToLower())
            {
                case "complete beginner":
                case "beginner":
                    skills.AddRange(new[] { "Basic Pronunciation", "Simple Phrases", "Greeting Conversations" });
                    break;
                case "elementary":
                    skills.AddRange(new[] { "Daily Conversations", "Basic Vocabulary", "Question Formation" });
                    break;
                case "pre-intermediate":
                case "intermediate":
                    skills.AddRange(new[] { "Fluency Building", "Complex Sentences", "Opinion Expression" });
                    break;
                case "upper-intermediate":
                case "advanced":
                    skills.AddRange(new[] { "Advanced Speaking", "Debates", "Presentations", "Cultural Fluency" });
                    break;
                case "near-native":
                    skills.AddRange(new[] { "Native-like Expression", "Subtle Nuances", "Advanced Discussions" });
                    break;
                default:
                    skills.AddRange(new[] { "Conversation Practice", "Pronunciation" });
                    break;
            }

            return skills.Distinct().ToList();
        }

        private string MapLevelToDifficulty(string? level)
        {
            return level?.ToLower() switch
            {
                "complete beginner" => "Very Easy",
                "beginner" => "Easy",
                "elementary" => "Easy-Medium",
                "pre-intermediate" => "Medium",
                "intermediate" => "Medium",
                "upper-intermediate" => "Medium-Hard",
                "advanced" => "Hard",
                "near-native" => "Very Hard",
                _ => "Medium"
            };
        }

        private List<string> GetDefaultSpeakingTopics()
        {
            return new List<string>
            {
                "Daily Conversation", "Pronunciation", "Basic Vocabulary", "Speaking Confidence",
                "Travel Conversations", "Business Speaking", "Social Interactions", "Cultural Communication"
            };
        }

        private AiCourseRecommendationDto CreateFallbackRecommendations(UserSurveyResponseDto survey)
        {
            return new AiCourseRecommendationDto
            {
                RecommendedCourses = new List<CourseRecommendationDto>(),
                ReasoningExplanation = $"Hiện tại chúng tôi đang chuẩn bị thêm nhiều khóa học {survey.PreferredLanguageName} speaking phù hợp với bạn. " +
                                     $"Dựa trên trình độ {survey.CurrentLevel} và mục tiêu {survey.TargetTimeline}, chúng tôi sẽ sớm có các khóa học phù hợp.",
                LearningPath = GetDefaultSpeakingLearningPath(survey.PreferredLanguageName, survey.CurrentLevel),
                StudyTips = GetDefaultSpeakingTips(survey.PreferredLanguageName, survey.PrioritySkills),
                GeneratedAt = DateTime.UtcNow
            };
        }

        private string GetDefaultSpeakingLearningPath(string languageName, string currentLevel)
        {
            return $@"
## 🎯 Lộ trình học Speaking {languageName} 

### 📊 Trình độ hiện tại: {currentLevel}

#### 🔥 Giai đoạn 1: Xây dựng nền tảng (Tuần 1-4)
- **Mục tiêu**: Phát âm cơ bản và cấu trúc câu đơn giản
- **Hoạt động**: Luyện phát âm, từ vựng cơ bản, câu chào hỏi

#### 💪 Giai đoạn 2: Phát triển kỹ năng (Tuần 5-12) 
- **Mục tiêu**: Giao tiếp trong tình huống hàng ngày
- **Hoạt động**: Hội thoại thực tế, mở rộng từ vựng

#### 🚀 Giai đoạn 3: Nâng cao và thành thạo (Tuần 13+)
- **Mục tiêu**: Tự tin giao tiếp trong mọi tình huống
- **Hoạt động**: Thảo luận phức tạp, thuyết trình, debates

💡 **Lời khuyên**: Luyện tập đều đặn mỗi ngày và đừng ngại mắc lỗi!
            ";
        }

        private List<string> GetDefaultSpeakingTips(string languageName, string? prioritySkills)
        {
            var tips = new List<string>
            {
                $"Luyện nói {languageName} ít nhất 20 phút mỗi ngày",
                "Ghi âm giọng nói của bạn để tự đánh giá và cải thiện",
                "Tìm speaking partner để luyện tập hội thoại thường xuyên",
                "Bắt chước phát âm của người bản ngữ qua video/audio",
                "Sử dụng ứng dụng speaking để luyện tập hàng ngày"
            };


            if (!string.IsNullOrEmpty(prioritySkills))
            {
                switch (prioritySkills.ToLower())
                {
                    case "pronunciation":
                        tips.Add("Tập trung vào từng âm vị và ngữ điệu câu");
                        tips.Add("Sử dụng mirror practice để quan sát cử động miệng");
                        break;
                    case "fluency":
                        tips.Add("Thực hành nói liên tục trong 1-2 phút không ngừng");
                        tips.Add("Đọc to báo hoặc sách để cải thiện tốc độ nói");
                        break;
                    case "confidence building":
                        tips.Add("Bắt đầu với các chủ đề bạn quen thuộc");
                        tips.Add("Tham gia các nhóm speaking online để tăng tự tin");
                        break;
                }
            }

            tips.AddRange(new[]
            {
                "Không ngại mắc lỗi - đó là cách học hiệu quả nhất",
                "Học từ vựng trong ngữ cảnh và sử dụng ngay",
                "Xem phim/video có phụ đề và lặp lại các câu thoại",
                "Tham gia các câu lạc bộ speaking hoặc language exchange"
            });

            return tips.Take(10).ToList();
        }

        private UserSurveyResponseDto MapToResponseDto(UserSurvey survey, Language? language)
        {
            AiCourseRecommendationDto? recommendations = null;

            if (!string.IsNullOrEmpty(survey.AiRecommendations))
            {
                try
                {
                    recommendations = JsonSerializer.Deserialize<AiCourseRecommendationDto>(survey.AiRecommendations);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize AI recommendations for survey {SurveyId}", survey.SurveyID);
                }
            }

            return new UserSurveyResponseDto
            {
                SurveyID = survey.SurveyID,
                CurrentLevel = survey.CurrentLevel,
                PreferredLanguageID = survey.PreferredLanguageID,
                PreferredLanguageName = language?.LanguageName ?? "",

                LearningReason = survey.LearningReason,
                PreviousExperience = survey.PreviousExperience,
                PreferredLearningStyle = survey.PreferredLearningStyle,
                InterestedTopics = survey.InterestedTopics,
                PrioritySkills = survey.PrioritySkills,
                TargetTimeline = survey.TargetTimeline,


                SpeakingChallenges = survey.SpeakingChallenges,
                ConfidenceLevel = survey.ConfidenceLevel,
                PreferredAccent = survey.PreferredAccent,

                IsCompleted = survey.IsCompleted,
                CreatedAt = survey.CreatedAt,
                AiRecommendations = recommendations
            };
        }
    }
}
