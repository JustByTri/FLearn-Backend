using BLL.IServices.AI;
using BLL.IServices.Assessment;
using BLL.IServices.Redis;
using Common.DTO.Assement;
using Common.DTO.Learner;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

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

        // ✅ NEW METHOD: Support multiple goals
        public async Task<VoiceAssessmentDto> StartVoiceAssessmentAsync(Guid userId, Guid languageId, List<int>? goalIds = null)
        {
            try
            {
                _logger.LogInformation("=== START VOICE ASSESSMENT WITH MULTIPLE GOALS ===");
                _logger.LogInformation("User ID: {UserId}, Language ID: {LanguageId}, Goal IDs: {GoalIds}",
                    userId, languageId, goalIds != null ? string.Join(",", goalIds) : "null");

                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                    throw new ArgumentException("Ngôn ngữ không tồn tại");

                var languageCode = language.LanguageCode?.Trim().ToUpper();
                var supportedLanguages = new[] { "EN", "ZH", "JA" };
                if (string.IsNullOrEmpty(languageCode) || !supportedLanguages.Contains(languageCode))
                    throw new ArgumentException("Chỉ hỗ trợ đánh giá giọng nói tiếng Anh, tiếng Trung và tiếng Nhật");

                // ✅ CHECK XEM USER ĐÃ ACCEPT ASSESSMENT CHƯA (có Roadmap)
                var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                var learnerLanguage = allLearnerLanguages.FirstOrDefault(ll =>
                    ll.UserId == userId && ll.LanguageId == languageId);

                if (learnerLanguage != null)
                {
                    var allRoadmaps = await _unitOfWork.Roadmaps.GetAllAsync();
                    var existingRoadmap = allRoadmaps.FirstOrDefault(r =>
                        r.LearnerLanguageId == learnerLanguage.LearnerLanguageId);

                    if (existingRoadmap != null)
                    {
                        _logger.LogWarning("❌ User {UserId} already ACCEPTED assessment for language {LanguageId}. Roadmap exists: {RoadmapId}",
                            userId, languageId, existingRoadmap.RoadmapID);

                        throw new InvalidOperationException(
                            $"Bạn đã hoàn thành và chấp nhận kết quả đánh giá cho {language.LanguageName}. " +
                            $"Không thể làm lại assessment sau khi đã chấp nhận kết quả.");
                    }
                }

                // ✅ LUÔN XÓA ASSESSMENT CŨ (nếu có)
                var existingAssessments = await _redisService.GetUserAssessmentsAsync(userId, languageId);
                var existingAssessment = existingAssessments.FirstOrDefault();

                if (existingAssessment != null)
                {
                    _logger.LogWarning("⚠️ Found existing assessment {AssessmentId}. Deleting to create fresh one...",
                        existingAssessment.AssessmentId);
                    await _redisService.DeleteVoiceAssessmentAsync(existingAssessment.AssessmentId);
                    _logger.LogInformation("✅ Deleted old assessment. Creating new one.");
                }

                // ✅ HANDLE MULTIPLE GOALS
                var goalNames = new List<string>();
                if (goalIds != null && goalIds.Any())
                {
                    foreach (var goalId in goalIds)
                    {
                        var goal = await _unitOfWork.Goals.GetByIdAsync(goalId);
                        if (goal != null)
                        {
                            goalNames.Add(goal.Name);
                        }
                    }
                    _logger.LogInformation("Goals selected: {GoalNames}", string.Join(", ", goalNames));
                }

                // ✅ GENERATE QUESTIONS
                _logger.LogInformation("🎯 Generating NEW questions for {LanguageCode} ({LanguageName})",
                    languageCode, language.LanguageName);

                var questions = await _geminiService.GenerateVoiceAssessmentQuestionsAsync(
                    languageCode, language.LanguageName);

                if (questions == null || questions.Count == 0)
                {
                    throw new InvalidOperationException($"Không thể tạo câu hỏi cho ngôn ngữ {language.LanguageName}");
                }

                _logger.LogInformation("📝 Generated {Count} questions", questions.Count);

                // ✅ CREATE ASSESSMENT WITH MULTIPLE GOALS
                var assessment = new VoiceAssessmentDto
                {
                    AssessmentId = Guid.NewGuid(),
                    UserId = userId,
                    LanguageId = languageId,
                    LanguageName = language.LanguageName,
                    GoalIds = goalIds ?? new List<int>(),
                    GoalNames = goalNames,
                    Questions = questions,
                    CreatedAt = DateTime.UtcNow,
                    CurrentQuestionIndex = 0
                };

                await _redisService.SetVoiceAssessmentAsync(assessment);

                _logger.LogInformation("✅ CREATED NEW Assessment {AssessmentId} with {GoalCount} goals",
                    assessment.AssessmentId, assessment.GoalIds.Count);

                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting voice assessment with multiple goals");
                throw;
            }
        }

        // ✅ BACKWARD COMPATIBILITY: Single goal support
        public async Task<VoiceAssessmentDto> StartVoiceAssessmentAsync(Guid userId, Guid languageId, int? goalId = null)
        {
            // Convert single goal to list and call the multiple goals method
            var goalIds = goalId.HasValue ? new List<int> { goalId.Value } : null;
            return await StartVoiceAssessmentAsync(userId, languageId, goalIds);
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

                // 🆕 CHECK FOR SKIPPED QUESTIONS BEFORE AI EVALUATION
                var completedQuestions = assessment.Questions.Where(q => !q.IsSkipped).ToList();
                var skippedCount = assessment.Questions.Count - completedQuestions.Count;

                BatchVoiceEvaluationResult result;

                if (completedQuestions.Count == 0)
                {
                    // 🆕 ALL QUESTIONS SKIPPED - CREATE DEFAULT RESULT
                    _logger.LogWarning("All questions skipped for assessment {AssessmentId}. Creating default beginner result.", assessmentId);

                    result = CreateDefaultSkippedResult(language.LanguageName, assessment.Questions.Count);
                }
                else if (skippedCount > completedQuestions.Count)
                {
                    // 🆕 MORE THAN HALF SKIPPED - EVALUATE WITH WARNING
                    _logger.LogInformation("More than half questions skipped ({SkippedCount}/{TotalCount}) for assessment {AssessmentId}",
                        skippedCount, assessment.Questions.Count, assessmentId);

                    result = await _geminiService.EvaluateBatchVoiceResponsesAsync(
                        assessment.Questions,
                        language.LanguageCode,
                        language.LanguageName);

                    // Adjust confidence and add warning message
                    result = AdjustResultForSkippedQuestions(result, skippedCount, assessment.Questions.Count);
                }
                else
                {
                    // 🆕 NORMAL EVALUATION - MOST QUESTIONS ANSWERED
                    result = await _geminiService.EvaluateBatchVoiceResponsesAsync(
                        assessment.Questions,
                        language.LanguageCode,
                        language.LanguageName);
                }

                var resultDto = MapBatchResultToDto(result, assessment, language);
                await _redisService.SetVoiceAssessmentResultAsync(assessment.UserId, assessment.LanguageId, resultDto);

                await CleanupAudioFilesAsync(assessment.Questions);

                _logger.LogInformation("✅ Completed assessment {AssessmentId} with level {Level} (Skipped: {SkippedCount}/{TotalCount})",
                    assessmentId, result.OverallLevel, skippedCount, assessment.Questions.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing voice assessment");
                throw;
            }
        }

        /// <summary>
        /// 🆕 Tạo kết quả mặc định khi tất cả câu hỏi đều bị skip
        /// </summary>
        private BatchVoiceEvaluationResult CreateDefaultSkippedResult(string languageName, int totalQuestions)
        {
            var beginnerLevel = GetBeginnerLevelForLanguage(languageName);

            return new BatchVoiceEvaluationResult
            {
                OverallLevel = beginnerLevel,
                OverallScore = 25,
                QuestionResults = new List<QuestionEvaluationResult>(),
                Strengths = new List<string>
                {
                    "Đã tham gia hoàn thành bài đánh giá",
                    "Sẵn sàng bắt đầu hành trình học ngôn ngữ"
                },
                Weaknesses = new List<string>
                {
                    "Cần thực hiện đánh giá giọng nói để có kết quả chính xác hơn",
                    "Nên thử trả lời một số câu hỏi để đánh giá khả năng hiện tại"
                },
                RecommendedCourses = GetBeginnerCoursesForLanguage(languageName),
                EvaluatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 🆕 Điều chỉnh kết quả khi có nhiều câu hỏi bị skip
        /// </summary>
        private BatchVoiceEvaluationResult AdjustResultForSkippedQuestions(
            BatchVoiceEvaluationResult originalResult,
            int skippedCount,
            int totalQuestions)
        {
            var skipPercentage = (double)skippedCount / totalQuestions * 100;

            // Reduce confidence and overall score due to limited data
            originalResult.OverallScore = Math.Max(10, originalResult.OverallScore - (int)(skipPercentage / 4));

            // Add warnings to feedback
            originalResult.Weaknesses = originalResult.Weaknesses ?? new List<string>();
            originalResult.Weaknesses.Insert(0, $"Đã bỏ qua {skippedCount}/{totalQuestions} câu hỏi, kết quả có thể chưa phản ánh đúng khả năng thực tế");

            if (skipPercentage >= 75)
            {
                originalResult.Weaknesses.Insert(0, "Nên thực hiện lại đánh giá với nhiều câu trả lời hơn để có kết quả chính xác");
            }

            return originalResult;
        }

        /// <summary>
        /// 🆕 Lấy level cơ bản cho từng ngôn ngữ
        /// </summary>
        private string GetBeginnerLevelForLanguage(string languageName)
        {
            if (languageName.Contains("English") || languageName.Contains("Anh"))
                return "A1";
            else if (languageName.Contains("Chinese") || languageName.Contains("Trung"))
                return "HSK 1";
            else if (languageName.Contains("Japanese") || languageName.Contains("Nhật"))
                return "N5";
            else
                return "Beginner";
        }

        /// <summary>
        /// 🆕 Tạo khuyến nghị khóa học cơ bản
        /// </summary>
        private List<CourseRecommendation> GetBeginnerCoursesForLanguage(string languageName)
        {
            if (languageName.Contains("English") || languageName.Contains("Anh"))
            {
                return new List<CourseRecommendation>
                {
                    new CourseRecommendation { Focus = "English Pronunciation Basics", Level = "A1", Reason = "Cần cải thiện phát âm cơ bản" },
                    new CourseRecommendation { Focus = "English Speaking Confidence", Level = "A1", Reason = "Xây dựng tự tin khi nói tiếng Anh" }
                };
            }
            else if (languageName.Contains("Chinese") || languageName.Contains("Trung"))
            {
                return new List<CourseRecommendation>
                {
                    new CourseRecommendation { Focus = "Chinese Pinyin & Tones", Level = "HSK 1", Reason = "Nắm vững hệ thống thanh điệu tiếng Trung" },
                    new CourseRecommendation { Focus = "Basic Chinese Conversation", Level = "HSK 1", Reason = "Học hội thoại tiếng Trung cơ bản" }
                };
            }
            else if (languageName.Contains("Japanese") || languageName.Contains("Nhật"))
            {
                return new List<CourseRecommendation>
                {
                    new CourseRecommendation { Focus = "Japanese Pronunciation & Hiragana", Level = "N5", Reason = "Học phát âm và bảng chữ Hiragana" },
                    new CourseRecommendation { Focus = "Basic Japanese Phrases", Level = "N5", Reason = "Học các cụm từ tiếng Nhật cơ bản" }
                };
            }

            return new List<CourseRecommendation>();
        }

        private string BuildDetailedFeedback(BatchVoiceEvaluationResult result, VoiceAssessmentDto assessment)
        {
            var completedCount = assessment.Questions.Count(q => !q.IsSkipped);
            var skippedCount = assessment.Questions.Count - completedCount;

            var feedback = $"**Tổng quan**: ";

            if (skippedCount == assessment.Questions.Count)
            {
                feedback += $"Bạn đã bỏ qua tất cả {assessment.Questions.Count} câu hỏi. ";
                feedback += $"Chúng tôi đánh giá bạn ở mức **{result.OverallLevel}** (mức cơ bản) ";
                feedback += $"để bạn có thể bắt đầu học từ nền tảng.\n\n";
                feedback += "**Khuyến nghị**: Hãy thử làm lại bài đánh giá và trả lời một số câu hỏi để có kết quả chính xác hơn.\n\n";
            }
            else if (skippedCount > 0)
            {
                feedback += $"Bạn đạt cấp độ **{result.OverallLevel}** với điểm tổng thể **{result.OverallScore}/100** ";
                feedback += $"(dựa trên {completedCount}/{assessment.Questions.Count} câu trả lời).\n\n";
                feedback += $"⚠️ **Lưu ý**: Bạn đã bỏ qua {skippedCount} câu hỏi, kết quả có thể chưa phản ánh đầy đủ khả năng thực tế.\n\n";
            }
            else
            {
                feedback += $"Bạn đạt cấp độ **{result.OverallLevel}** với điểm tổng thể **{result.OverallScore}/100** ";
                feedback += $"(hoàn thành đầy đủ {assessment.Questions.Count} câu hỏi).\n\n";
            }

            if (result.QuestionResults.Any())
            {
                feedback += "**Chi tiết từng câu trả lời**:\n";
                foreach (var qr in result.QuestionResults)
                {
                    feedback += $"- Câu {qr.QuestionNumber}: {qr.Feedback}\n";
                    if (qr.MissingWords.Any())
                    {
                        feedback += $"  ⚠️ Thiếu từ: {string.Join(", ", qr.MissingWords)}\n";
                    }
                }
            }

            return feedback;
        }

        private VoiceAssessmentResultDto MapBatchResultToDto(
            BatchVoiceEvaluationResult batchResult,
            VoiceAssessmentDto assessment,
            DAL.Models.Language language)
        {
            var completedCount = assessment.Questions.Count(q => !q.IsSkipped);
            var isFullySkipped = completedCount == 0;

            return new VoiceAssessmentResultDto
            {
                AssessmentId = assessment.AssessmentId,
                LanguageName = language.LanguageName,
                DeterminedLevel = batchResult.OverallLevel,
                LevelConfidence = isFullySkipped ? 30 : (completedCount < assessment.Questions.Count / 2 ? 60 : 95),
                AssessmentCompleteness = $"{completedCount}/{assessment.Questions.Count} câu" +
                    (isFullySkipped ? " (tất cả đều bỏ qua)" : ""),
                OverallScore = batchResult.OverallScore,

                // Calculate average scores from question results
                PronunciationScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.PronunciationScore)
                    : (isFullySkipped ? 20 : 0), // Give some base score if all skipped

                FluencyScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.FluencyScore)
                    : (isFullySkipped ? 20 : 0),

                GrammarScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.GrammarScore)
                    : (isFullySkipped ? 20 : 0),

                VocabularyScore = batchResult.QuestionResults.Any()
                    ? (int)batchResult.QuestionResults.Average(q => q.AccuracyScore)
                    : (isFullySkipped ? 20 : 0),

                DetailedFeedback = BuildDetailedFeedback(batchResult, assessment), // Updated method
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
                    GoalName = assessment.GoalName // Using the computed property
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

        /// <summary>
        /// Lưu danh sách khóa học được gợi ý vào Redis để sử dụng khi accept assessment
        /// </summary>
        public async Task SaveRecommendedCoursesAsync(
            Guid userId,
            Guid languageId,
            List<CourseRecommendationDto> courses)
        {
            try
            {
                if (courses == null || !courses.Any())
                {
                    _logger.LogWarning("No courses to save for user {UserId}, language {LanguageId}",
                        userId, languageId);
                    return;
                }

                // Tạo key để lưu trong Redis
                var cacheKey = $"voice_assessment_recommended_courses:{userId}:{languageId}";

                // Lưu vào Redis với TTL 24 giờ
                await _redisService.SetAsync(cacheKey, courses, TimeSpan.FromHours(24));

                _logger.LogInformation("✅ Saved {Count} recommended courses to Redis for user {UserId}, language {LanguageId}",
                    courses.Count, userId, languageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving recommended courses to Redis for user {UserId}", userId);
                // Không throw - cho phép tiếp tục flow
            }
        }

        public async Task<int> ClearAllAssessmentsDebugAsync()
        {
            return await _redisService.ClearAllAssessmentsAsync();
        }
    }
}

