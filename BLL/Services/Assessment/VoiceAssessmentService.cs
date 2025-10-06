using BLL.IServices.AI;
using BLL.IServices.Assessment;

using BLL.IServices.Redis;
using BLL.Services.UserGoal;
using Common.DTO.Assement;
using Common.DTO.Learner;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Assessment
{
    public class VoiceAssessmentService : IVoiceAssessmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeminiService _geminiService;
        private readonly IRedisService _redisService;
        private readonly ILogger<VoiceAssessmentService> _logger;

        public VoiceAssessmentService(
            IUnitOfWork unitOfWork,
            IGeminiService geminiService,
            IRedisService redisService,
            ILogger<VoiceAssessmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _geminiService = geminiService;
            _redisService = redisService;
            _logger = logger;
        }
        public async Task ClearAssessmentResultAsync(Guid userId, Guid languageId)
        {
            try
            {
                // Xóa kết quả assessment cũ từ Redis
                await _redisService.DeleteVoiceAssessmentResultAsync(userId, languageId);

                // Xóa assessment đang active (nếu có)
                var activeAssessments = await _redisService.GetUserAssessmentsAsync(userId, languageId);
                foreach (var assessment in activeAssessments)
                {
                    await _redisService.DeleteVoiceAssessmentAsync(assessment.AssessmentId);
                }

                _logger.LogInformation("🗑️ Cleared assessment results for user {UserId}, language {LanguageId}",
                    userId, languageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing assessment results for user {UserId}", userId);
                // Không throw - cho phép tiếp tục
            }
        }
        public async Task<VoiceAssessmentDto> StartVoiceAssessmentAsync(Guid userId, Guid languageId, int? goalId = null)
        {
            try
            {
                _logger.LogInformation("=== START VOICE ASSESSMENT (Redis) ===");
                _logger.LogInformation("User ID: {UserId}, Language ID: {LanguageId}, Goal ID: {GoalId}",
                    userId, languageId, goalId);

                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                    throw new ArgumentException("Ngôn ngữ không tồn tại");

                var supportedLanguages = new[] { "EN", "ZH", "JP" };
                if (!supportedLanguages.Contains(language.LanguageCode))
                    throw new ArgumentException("Chỉ hỗ trợ đánh giá giọng nói tiếng Anh, tiếng Trung và tiếng Nhật");

                var existingAssessments = await _redisService.GetUserAssessmentsAsync(userId, languageId);
                var existingAssessment = existingAssessments.FirstOrDefault();

                if (existingAssessment != null)
                {
                    _logger.LogInformation("Resuming existing assessment {AssessmentId}", existingAssessment.AssessmentId);
                    return existingAssessment;
                }

                // Lấy Goal nếu có
                string? goalName = null;
                if (goalId.HasValue)
                {
                    var goal = await _unitOfWork.Goals.GetByIdAsync(goalId.Value);
                    goalName = goal?.Name;
                    _logger.LogInformation("Goal selected: {GoalName}", goalName ?? "None");
                }

                var questions = await _geminiService.GenerateVoiceAssessmentQuestionsAsync(
                    language.LanguageCode,
                    language.LanguageName);

                var assessment = new VoiceAssessmentDto
                {
                    AssessmentId = Guid.NewGuid(),
                    UserId = userId,
                    LanguageId = languageId,
                    LanguageName = language.LanguageName,
                    GoalID = goalId,
                    GoalName = goalName,
                    Questions = questions,
                    CreatedAt = DateTime.UtcNow,
                    CurrentQuestionIndex = 0
                };

                await _redisService.SetVoiceAssessmentAsync(assessment);

                _logger.LogInformation("✅ CREATED Assessment {AssessmentId} with Goal: {GoalName}",
                    assessment.AssessmentId, goalName ?? "None");

                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting voice assessment");
                throw;
            }
        }

        public async Task<VoiceAssessmentQuestion> GetCurrentQuestionAsync(Guid assessmentId)
        {
            try
            {
                _logger.LogInformation("=== GET CURRENT QUESTION (Redis) ===");

                var assessment = await _redisService.GetVoiceAssessmentAsync(assessmentId);

                if (assessment == null)
                {
                    _logger.LogWarning("Assessment {AssessmentId} not found in Redis", assessmentId);
                    throw new ArgumentException("Voice assessment không tồn tại hoặc đã hết hạn");
                }

                if (assessment.CurrentQuestionIndex >= assessment.Questions.Count)
                {
                    throw new ArgumentException("Đã hoàn thành tất cả câu hỏi. Vui lòng gọi CompleteAssessment.");
                }

                var currentQuestion = assessment.Questions[assessment.CurrentQuestionIndex];
                return currentQuestion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current question for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        public async Task SubmitVoiceResponseAsync(Guid assessmentId, VoiceAssessmentResponseDto response)
        {
            try
            {
                _logger.LogInformation("📤 Submitting voice response for assessment {AssessmentId}, question {QuestionNumber}",
                    assessmentId, response.QuestionNumber);

                var assessment = await _redisService.GetVoiceAssessmentAsync(assessmentId);
                if (assessment == null)
                    throw new ArgumentException("Assessment không tồn tại");

                var question = assessment.Questions.FirstOrDefault(q => q.QuestionNumber == response.QuestionNumber);
                if (question == null)
                    throw new ArgumentException("Câu hỏi không tồn tại");

                // Mark as skipped or save audio
                question.IsSkipped = response.IsSkipped;

                if (!response.IsSkipped && response.AudioFile != null)
                {
                    // Save audio file to temp storage
                    var audioPath = await SaveAudioFileAsync(response.AudioFile, assessmentId, response.QuestionNumber);
                    question.AudioFilePath = audioPath;

                    _logger.LogInformation("✅ Saved audio file: {Path}", audioPath);
                }

                // Move to next question
                assessment.CurrentQuestionIndex++;

                // Update Redis
                await _redisService.SetVoiceAssessmentAsync(assessment);

                _logger.LogInformation("✅ Question {QuestionNumber} submitted. Moving to {NextIndex}",
                    response.QuestionNumber, assessment.CurrentQuestionIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting voice response");
                throw;
            }
        }

        public async Task<BatchVoiceEvaluationResult> CompleteVoiceAssessmentAsync(Guid assessmentId)
        {
            try
            {
                _logger.LogInformation("🏁 Completing voice assessment {AssessmentId}", assessmentId);

                var assessment = await _redisService.GetVoiceAssessmentAsync(assessmentId);
                if (assessment == null)
                {
                    _logger.LogWarning("Assessment {AssessmentId} not found in Redis", assessmentId);
                    throw new ArgumentException("Assessment không tồn tại hoặc đã hết hạn");
                }

                var language = await _unitOfWork.Languages.GetByIdAsync(assessment.LanguageId);
                if (language == null)
                    throw new ArgumentException("Ngôn ngữ không tồn tại");

                // Evaluate
                var result = await _geminiService.EvaluateBatchVoiceResponsesAsync(
                    assessment.Questions,
                    language.LanguageCode,
                    language.LanguageName);

           
                var resultDto = MapBatchResultToDto(result, assessment, language);
                await _redisService.SetVoiceAssessmentResultAsync(assessment.UserId, assessment.LanguageId, resultDto);

                await CleanupAudioFilesAsync(assessment.Questions);

                _logger.LogInformation("✅ Completed assessment {AssessmentId} with level {Level}",
                    assessmentId, result.OverallLevel);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing voice assessment");
                throw;
            }
        }

        private async Task CreatePendingVoiceAssessmentAsync(Guid userId, VoiceAssessmentResultDto resultDto)
        {
            try
            {
                // ✅ Create logger with correct type
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var userGoalLogger = loggerFactory.CreateLogger<UserGoalService>();

                var userGoalService = new UserGoalService(_unitOfWork, userGoalLogger);
                await userGoalService.CreatePendingVoiceAssessmentResultAsync(userId, resultDto);

                _logger.LogInformation("✅ Created pending voice assessment result for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pending voice assessment for user {UserId}", userId);
            }
        }
        private VoiceAssessmentResultDto MapBatchResultToDto(
    BatchVoiceEvaluationResult batchResult,
    VoiceAssessmentDto assessment,
    DAL.Models.Language language)
        {
            return new VoiceAssessmentResultDto
            {
                AssessmentId = assessment.AssessmentId,
                LanguageName = language.LanguageName,
                DeterminedLevel = batchResult.OverallLevel,
                LevelConfidence = 95, // High confidence cho batch evaluation
                AssessmentCompleteness = $"{assessment.Questions.Count(q => !q.IsSkipped)}/{assessment.Questions.Count} câu",
                OverallScore = batchResult.OverallScore,

                // Calculate average scores from question results
                PronunciationScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.PronunciationScore)
                    : 0,
                FluencyScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.FluencyScore)
                    : 0,
                GrammarScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.GrammarScore)
                    : 0,
                VocabularyScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.AccuracyScore)
                    : 0,

                DetailedFeedback = BuildDetailedFeedback(batchResult),
                KeyStrengths = batchResult.Strengths,
                ImprovementAreas = batchResult.Weaknesses,
                NextLevelRequirements = GetNextLevelRequirement(batchResult.OverallLevel, language.LanguageName),

                // Map course recommendations
                RecommendedCourses = batchResult.RecommendedCourses.Select(rc => new RecommendedCourseDto
                {
                    CourseId = rc.CourseId ?? Guid.Empty,
                    CourseName = rc.Focus,
                    Level = rc.Level,
                    MatchReason = rc.Reason,
                    GoalName = assessment.GoalName
                }).ToList(),

                CompletedAt = DateTime.UtcNow
            };
        }
        private string GetNextLevelRequirement(string currentLevel, string languageName)
        {
            if (languageName.Contains("Anh") || languageName.Contains("English"))
            {
                return currentLevel switch
                {
                    "A1" => "Để đạt A2: Học 500-700 từ mới, luyện past tense, cải thiện fluency lên 80-100 wpm",
                    "A2" => "Để đạt B1: Học 1000+ từ, master all tenses, luyện speaking 100-120 wpm",
                    "B1" => "Để đạt B2: Vocabulary 3500+, advanced grammar, fluency 120-140 wpm",
                    "B2" => "Để đạt C1: Sophisticated vocabulary, complex structures, near-native fluency",
                    "C1" => "Để đạt C2: Native-like proficiency in all aspects",
                    _ => "Hoàn thành đánh giá đầy đủ để biết yêu cầu cụ thể"
                };
            }
            else if (languageName.Contains("Trung") || languageName.Contains("Chinese"))
            {
                return currentLevel switch
                {
                    "HSK 1" => "Để đạt HSK 2: Học thêm 300 từ, master 4 thanh, luyện tập hội thoại đơn giản",
                    "HSK 2" => "Để đạt HSK 3: Học 600+ từ mới, cải thiện độ chính xác thanh điệu lên 80%+",
                    "HSK 3" => "Để đạt HSK 4: Vocabulary 1200-2500 từ, chengyu cơ bản, fluency tốt",
                    "HSK 4" => "Để đạt HSK 5: 2500+ từ, chengyu nâng cao, đọc báo Trung Quốc",
                    "HSK 5" => "Để đạt HSK 6: 5000+ từ, văn học cổ điển, thành ngữ native",
                    _ => "Hoàn thành đánh giá đầy đủ để biết yêu cầu cụ thể"
                };
            }
            else if (languageName.Contains("Nhật") || languageName.Contains("Japanese"))
            {
                return currentLevel switch
                {
                    "N5" => "Để đạt N4: Học 700+ từ mới, 200 kanji, master て-form và basic conjugations",
                    "N4" => "Để đạt N3: 1500+ từ mới, 350 kanji, cải thiện pitch accent, keigo cơ bản",
                    "N3" => "Để đạt N2: 3000+ từ, 650 kanji, business Japanese, advanced grammar",
                    "N2" => "Để đạt N1: 6000+ từ, 1000+ kanji, literary forms, native-like keigo",
                    _ => "Hoàn thành đánh giá đầy đủ để biết yêu cầu cụ thể"
                };
            }

            return "Hoàn thành bài test để nhận lộ trình học tập chi tiết";
        }
        private string BuildDetailedFeedback(BatchVoiceEvaluationResult result)
        {
            var feedback = $"**Tổng quan**: Bạn đạt cấp độ **{result.OverallLevel}** với điểm tổng thể **{result.OverallScore}/100**.\n\n";

            feedback += "**Chi tiết từng câu**:\n";
            foreach (var qr in result.QuestionResults)
            {
                feedback += $"- Câu {qr.QuestionNumber}: {qr.Feedback}\n";
                if (qr.MissingWords.Any())
                {
                    feedback += $"  ⚠️ Thiếu từ: {string.Join(", ", qr.MissingWords)}\n";
                }
            }

            return feedback;
        }


        private async Task<string> SaveAudioFileAsync(IFormFile audioFile, Guid assessmentId, int questionNumber)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "voice-assessments");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{assessmentId}_{questionNumber}_{Guid.NewGuid()}.mp3";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            return filePath;
        }

        private async Task CleanupAudioFilesAsync(List<VoiceAssessmentQuestion> questions)
        {
            foreach (var question in questions.Where(q => !string.IsNullOrEmpty(q.AudioFilePath)))
            {
                try
                {
                    if (File.Exists(question.AudioFilePath))
                    {
                        File.Delete(question.AudioFilePath);
                        _logger.LogInformation("🗑️ Deleted audio file: {Path}", question.AudioFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete audio file: {Path}", question.AudioFilePath);
                }
            }
        }

        private async Task EnrichWithCourseRecommendationsAsync(
            Guid userId,
            BatchVoiceEvaluationResult result,
            Guid languageId)
        {
            try
            {
                // Get available courses based on weaknesses
                var weaknessFoci = result.RecommendedCourses.Select(c => c.Focus).ToList();
                var courses = await GetCoursesForWeaknessesAsync(languageId, weaknessFoci, result.OverallLevel);

                // Map to result
                result.RecommendedCourses = courses.Select(c => new CourseRecommendation
                {
                    Focus = c.Title,
                    Reason = $"Khóa học này phù hợp với level {result.OverallLevel}",
                    Level = c.Level
                }).Take(3).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not enrich with course recommendations");
            }
        }

        private async Task<List<CourseInfoDto>> GetCoursesForWeaknessesAsync(
            Guid languageId,
            List<string> weaknessFoci,
            string level)
        {
            var courses = await _unitOfWork.Courses.GetCoursesByLanguageAsync(languageId);

            // Filter by level and relevant topics
            return courses
                .Where(c => c.Level == level || c.Level == "All Levels")
                .Take(3)
                .Select(c => new CourseInfoDto
                {
                    CourseID = c.CourseID,
                    Title = c.Title,
                    Description = c.Description ?? "",
                    Level = c.Level ?? level
                })
                .ToList();
        }

        /// <summary>
        /// ✅ THÊM METHOD MỚI: Generate course recommendations cho voice assessment result
        /// </summary>
        private async Task GenerateCourseRecommendationsForResult(Guid userId, VoiceAssessmentResultDto result)
        {
            try
            {
              
                var survey = await _unitOfWork.UserSurveys.GetByUserIdAsync(userId);
                if (survey == null)
                {
                    _logger.LogInformation("User {UserId} has no survey - skipping course recommendations", userId);
                    return;
                }

             
                var availableCourses = await GetAvailableCoursesForLanguage(result.LanguageName, result.DeterminedLevel);

                if (!availableCourses.Any())
                {
                    _logger.LogWarning("No courses available for language {Language} and level {Level}",
                        result.LanguageName, result.DeterminedLevel);
                    return;
                }

            
                var surveyDto = BuildUserSurveyDto(survey, result);

         
                var aiRecommendations = await _geminiService.GenerateCourseRecommendationsAsync(surveyDto, availableCourses);

            
                result.RecommendedCourses = aiRecommendations.RecommendedCourses?
                    .Take(5) 
                    .Select(c => new RecommendedCourseDto
                    {
                        CourseId = c.CourseID,
                        CourseName = c.CourseName,
                        Level = c.Level,
                        MatchReason = c.MatchReason,
                        GoalName = result.LanguageName 
                    })
                    .ToList();

                _logger.LogInformation("Generated {Count} course recommendations based on survey + voice level {Level}",
                    result.RecommendedCourses.Count, result.DeterminedLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating course recommendations for user {UserId}", userId);
                throw;
            }
        }

     
        private async Task<List<CourseInfoDto>> GetAvailableCoursesForLanguage(string languageName, string determinedLevel)
        {
            try
            {
             
                var language = await _unitOfWork.Languages.GetByNameAsync(languageName);
                if (language == null) return new List<CourseInfoDto>();

             
                var courses = await _unitOfWork.Courses.GetCoursesByLanguageAsync(language.LanguageID);

          
                return courses.Select(c => new CourseInfoDto
                {
                    CourseID = c.CourseID,
                    Title = c.Title,
                    Description = c.Description ?? "",
                    Level = MapVoiceLevelToCourseLevel(determinedLevel), 
                    Language = languageName,
                  
                    Difficulty = c.Level ?? "Beginner",
                    Skills = new List<string> { "Speaking", "Listening", "Grammar", "Vocabulary" }, 
                    Topics = new List<string>()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available courses for language {Language}", languageName);
                return new List<CourseInfoDto>();
            }
        }

     
        private string MapVoiceLevelToCourseLevel(string voiceLevel)
        {
            return voiceLevel.ToUpper() switch
            {
             
                "A1" or "A2" => "Beginner",
                "B1" => "Intermediate",
                "B2" => "Upper-Intermediate",
                "C1" or "C2" => "Advanced",

              
                "HSK 1" or "HSK 2" => "Beginner",
                "HSK 3" or "HSK 4" => "Intermediate",
                "HSK 5" or "HSK 6" => "Advanced",

               
                "N5" or "N4" => "Beginner",
                "N3" or "N2" => "Intermediate",
                "N1" => "Advanced",

           
                _ => "Beginner"
            };
        }

 
        private UserSurveyResponseDto BuildUserSurveyDto(UserSurvey survey, VoiceAssessmentResultDto voiceResult)
        {
            return new UserSurveyResponseDto
            {
                SurveyID = survey.SurveyID,
                CurrentLevel = voiceResult.DeterminedLevel,
                PreferredLanguageID = survey.PreferredLanguageID,
                PreferredLanguageName = voiceResult.LanguageName,
                LearningReason = survey.LearningReason,
                PreviousExperience = survey.PreviousExperience,
                PreferredLearningStyle = survey.PreferredLearningStyle,
                InterestedTopics = survey.InterestedTopics,
                PrioritySkills = survey.PrioritySkills + ", Speaking", 
                TargetTimeline = survey.TargetTimeline,
                SpeakingChallenges = survey.SpeakingChallenges,
                PreferredAccent = survey.PreferredAccent,
                CreatedAt = survey.CreatedAt
            };
        }
        public async Task<VoiceAssessmentResultDto?> GetVoiceAssessmentResultAsync(Guid userId, Guid languageId)
        {
            return await _redisService.GetVoiceAssessmentResultAsync(userId, languageId);
        }

        public async Task<bool> HasCompletedVoiceAssessmentAsync(Guid userId, Guid languageId)
        {
            var result = await _redisService.GetVoiceAssessmentResultAsync(userId, languageId);
            return result != null;
        }

   
        public async Task<List<VoiceAssessmentDto>> GetActiveAssessmentsDebugAsync()
        {
            return await _redisService.GetActiveVoiceAssessmentsAsync();
        }

        public async Task<Guid?> FindAssessmentIdAsync(Guid userId, Guid languageId)
        {
            var assessments = await _redisService.GetUserAssessmentsAsync(userId, languageId);
            return assessments.FirstOrDefault()?.AssessmentId;
        }

        public async Task<VoiceAssessmentDto?> RestoreAssessmentFromIdAsync(Guid assessmentId)
        {
            return await _redisService.GetVoiceAssessmentAsync(assessmentId);
        }

        public async Task<bool> ValidateAssessmentIdAsync(Guid assessmentId, Guid userId)
        {
            var assessment = await _redisService.GetVoiceAssessmentAsync(assessmentId);
            return assessment != null && assessment.UserId == userId;
        }

        public async Task<bool> DeleteAssessmentDebugAsync(Guid assessmentId)
        {
            return await _redisService.DeleteVoiceAssessmentAsync(assessmentId);
        }

        public async Task<int> ClearAllAssessmentsDebugAsync()
        {
            return await _redisService.ClearAllAssessmentsAsync();
        }

        //private void CalculateDetailedScores(VoiceAssessmentResultDto result, List<VoiceAssessmentQuestion> questions)
        //{
        //    var completedQuestions = questions.Where(q => !q.IsSkipped && q.EvaluationResult != null).ToList();

        //    if (completedQuestions.Any())
        //    {
        //        result.PronunciationScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Pronunciation.Score);
        //        result.FluencyScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Fluency.Score);
        //        result.GrammarScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Grammar.Score);
        //        result.VocabularyScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Vocabulary.Score);
        //        result.OverallScore = (int)completedQuestions.Average(q => q.EvaluationResult!.OverallScore);

        //        var allStrengths = new List<string>();
        //        var allImprovements = new List<string>();

        //        foreach (var q in completedQuestions)
        //        {
        //            if (q.EvaluationResult?.Strengths != null)
        //                allStrengths.AddRange(q.EvaluationResult.Strengths);

        //            if (q.EvaluationResult?.AreasForImprovement != null)
        //                allImprovements.AddRange(q.EvaluationResult.AreasForImprovement);
        //        }

        //        result.KeyStrengths = allStrengths.Distinct().Take(5).ToList();
        //        result.ImprovementAreas = allImprovements.Distinct().Take(5).ToList();
        //    }
        //    else
        //    {
        //        result.PronunciationScore = 0;
        //        result.FluencyScore = 0;
        //        result.GrammarScore = 0;
        //        result.VocabularyScore = 0;
        //        result.OverallScore = 0;
        //        result.DeterminedLevel = "Unassessed";
        //        result.DetailedFeedback = "Không thể đánh giá vì tất cả câu hỏi đều được bỏ qua.";
        //        result.KeyStrengths = new List<string> { "Tham gia hoàn thành bài test" };
        //        result.ImprovementAreas = new List<string> { "Nên trả lời các câu hỏi để có đánh giá chính xác" };
        //    }
        //}
    }
}
