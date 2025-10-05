using BLL.IServices.AI;
using BLL.IServices.Assessment;

using BLL.IServices.Redis;
using Common.DTO.Assement;
using Common.DTO.Learner;
using DAL.Models;
using DAL.UnitOfWork;
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
                _logger.LogInformation("=== SUBMIT VOICE RESPONSE (Redis) ===");
                _logger.LogInformation("Assessment ID: {AssessmentId}", assessmentId);
                _logger.LogInformation("Question Number: {QuestionNumber}, Is Skipped: {IsSkipped}",
                    response.QuestionNumber, response.IsSkipped);

         
                var assessment = await _redisService.GetVoiceAssessmentAsync(assessmentId);

                if (assessment == null)
                {
                    _logger.LogError("❌ Assessment {AssessmentId} NOT FOUND in Redis", assessmentId);
                    throw new ArgumentException("Voice assessment không tồn tại");
                }

                _logger.LogInformation("✅ Assessment found in Redis: User={UserId}, Language={LanguageName}, CurrentIndex={Index}",
                    assessment.UserId, assessment.LanguageName, assessment.CurrentQuestionIndex);

                var question = assessment.Questions.FirstOrDefault(q => q.QuestionNumber == response.QuestionNumber);
                if (question == null)
                {
                    _logger.LogError("Question {QuestionNumber} not found. Available questions: [{Questions}]",
                        response.QuestionNumber,
                        string.Join(", ", assessment.Questions.Select(q => q.QuestionNumber)));
                    throw new ArgumentException("Câu hỏi không tồn tại");
                }

         
                question.IsSkipped = response.IsSkipped;

                if (!response.IsSkipped && response.AudioFile != null)
                {
                    _logger.LogInformation("Processing audio file: {FileName}, Size: {Size} bytes",
                        response.AudioFile.FileName, response.AudioFile.Length);

                 
                    var language = await _unitOfWork.Languages.GetByIdAsync(assessment.LanguageId);
                    if (language != null)
                    {
                
                        _logger.LogInformation("Starting AI evaluation for question {QuestionNumber}...", response.QuestionNumber);

                        question.EvaluationResult = await _geminiService.EvaluateVoiceResponseDirectlyAsync(
                            question,
                            response.AudioFile,
                            language.LanguageCode);

                        _logger.LogInformation("AI evaluation completed for question {QuestionNumber}, score: {Score}",
                            response.QuestionNumber, question.EvaluationResult.OverallScore);
                    }
                }
                else
                {
                    _logger.LogInformation("Question {QuestionNumber} was skipped", response.QuestionNumber);


                    question.EvaluationResult = new VoiceEvaluationResult
                    {
                        OverallScore = 0,
                        DetailedFeedback = "Câu hỏi đã được bỏ qua",
                        Pronunciation = new PronunciationScore { Score = 0, Level = "Skipped", Feedback = "Đã bỏ qua" },
                        Fluency = new FluencyScore { Score = 0, Rhythm = "Skipped", Feedback = "Đã bỏ qua" },
                        Grammar = new GrammarScore { Score = 0, Feedback = "Đã bỏ qua" },
                        Vocabulary = new VocabularyScore { Score = 0, RangeAssessment = "Skipped", Feedback = "Đã bỏ qua" },
                        Strengths = new List<string>(),
                        AreasForImprovement = new List<string> { "Nên thử trả lời câu hỏi để có đánh giá chính xác" }
                    };
                }

                var previousIndex = assessment.CurrentQuestionIndex;
                assessment.CurrentQuestionIndex++;


                await _redisService.SetVoiceAssessmentAsync(assessment);

                _logger.LogInformation("✅ UPDATED Redis: Assessment {AssessmentId} moved from question {Previous} to {Current}",
                    assessmentId, previousIndex, assessment.CurrentQuestionIndex);

                _logger.LogInformation("=== SUBMIT VOICE RESPONSE SUCCESS (Redis) ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SUBMIT VOICE RESPONSE ERROR (Redis) === for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        public async Task<VoiceAssessmentResultDto> CompleteVoiceAssessmentAsync(Guid assessmentId)
        {
            try
            {
                var assessment = await _redisService.GetVoiceAssessmentAsync(assessmentId);

                if (assessment == null)
                    throw new ArgumentException("Voice assessment không tồn tại");

                var language = await _unitOfWork.Languages.GetByIdAsync(assessment.LanguageId);
                if (language == null)
                    throw new ArgumentException("Ngôn ngữ không tồn tại");

          
                var result = await _geminiService.GenerateVoiceAssessmentResultAsync(
                    language.LanguageCode,
                    language.LanguageName,
                    assessment.Questions,
                    assessment.GoalName);

                result.AssessmentId = assessmentId;
                result.LanguageName = language.LanguageName;
                result.CompletedAt = DateTime.UtcNow;

        
                CalculateDetailedScores(result, assessment.Questions);

            
                try
                {
                    await GenerateCourseRecommendationsForResult(assessment.UserId, result);
                    _logger.LogInformation("✅ Generated {Count} course recommendations for user {UserId}",
                        result.RecommendedCourses?.Count ?? 0, assessment.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not generate course recommendations for user {UserId}. Continuing without recommendations.", assessment.UserId);
                  
                }

  
                await _redisService.SetVoiceAssessmentResultAsync(assessment.UserId, assessment.LanguageId, result);

        
                await _redisService.DeleteVoiceAssessmentAsync(assessmentId);

                _logger.LogInformation("✅ COMPLETED and SAVED to Redis: Assessment {AssessmentId} with level {Level}, {CourseCount} recommended courses",
                    assessmentId, result.DeterminedLevel, result.RecommendedCourses?.Count ?? 0);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing voice assessment {AssessmentId}", assessmentId);
                throw;
            }
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

        private void CalculateDetailedScores(VoiceAssessmentResultDto result, List<VoiceAssessmentQuestion> questions)
        {
            var completedQuestions = questions.Where(q => !q.IsSkipped && q.EvaluationResult != null).ToList();

            if (completedQuestions.Any())
            {
                result.PronunciationScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Pronunciation.Score);
                result.FluencyScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Fluency.Score);
                result.GrammarScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Grammar.Score);
                result.VocabularyScore = (int)completedQuestions.Average(q => q.EvaluationResult!.Vocabulary.Score);
                result.OverallScore = (int)completedQuestions.Average(q => q.EvaluationResult!.OverallScore);

                var allStrengths = new List<string>();
                var allImprovements = new List<string>();

                foreach (var q in completedQuestions)
                {
                    if (q.EvaluationResult?.Strengths != null)
                        allStrengths.AddRange(q.EvaluationResult.Strengths);

                    if (q.EvaluationResult?.AreasForImprovement != null)
                        allImprovements.AddRange(q.EvaluationResult.AreasForImprovement);
                }

                result.KeyStrengths = allStrengths.Distinct().Take(5).ToList();
                result.ImprovementAreas = allImprovements.Distinct().Take(5).ToList();
            }
            else
            {
                result.PronunciationScore = 0;
                result.FluencyScore = 0;
                result.GrammarScore = 0;
                result.VocabularyScore = 0;
                result.OverallScore = 0;
                result.DeterminedLevel = "Unassessed";
                result.DetailedFeedback = "Không thể đánh giá vì tất cả câu hỏi đều được bỏ qua.";
                result.KeyStrengths = new List<string> { "Tham gia hoàn thành bài test" };
                result.ImprovementAreas = new List<string> { "Nên trả lời các câu hỏi để có đánh giá chính xác" };
            }
        }
    }
}
