using BLL.IServices.AI;
using BLL.IServices.Assessment;
using BLL.IServices.Redis;
using Common.DTO.Assement;
using Common.DTO.Learner;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
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

        // 1. Bắt đầu bài đánh giá
        public async Task<VoiceAssessmentDto> StartProgramAssessmentAsync(Guid userId, Guid languageId, Guid programId)
        {
            _logger.LogInformation("Bắt đầu Program Assessment: User {UserId}, Program {ProgramId}", userId, programId);

            var program = await _unitOfWork.Programs.GetByIdAsync(programId);
            if (program == null || program.LanguageId != languageId)
                throw new ArgumentException("Khung chương trình không hợp lệ.");

            var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
            if (language == null)
                throw new ArgumentException("Ngôn ngữ không tồn tại.");

            var allLevels = await _unitOfWork.Levels.GetAllAsync();
            var programLevelNames = allLevels
                .Where(l => l.ProgramId == programId)
                .OrderBy(l => l.OrderIndex)
                .Select(l => l.Name)
                .ToList();

            if (!programLevelNames.Any())
                throw new InvalidOperationException("Khung chương trình này chưa có cấp độ (level).");

            await ClearOldAssessmentData(userId, languageId);

            var questions = await _geminiService.GenerateVoiceAssessmentQuestionsAsync(
                language.LanguageCode,
                language.LanguageName,
                program.Name
            );

            if (questions == null || !questions.Any())
                throw new InvalidOperationException($"Không thể tạo câu hỏi cho {program.Name}");

            var assessment = new VoiceAssessmentDto
            {
                AssessmentId = Guid.NewGuid(),
                UserId = userId,
                LanguageId = languageId,
                LanguageName = language.LanguageName,
                Questions = questions,
                CreatedAt = DateTime.UtcNow,
                CurrentQuestionIndex = 0,
                ProgramId = programId,
                ProgramName = program.Name,
                ProgramLevelNames = programLevelNames,
              
            };

            await _redisService.SetVoiceAssessmentAsync(assessment);
            return assessment;
        }

        // 2. Lấy câu hỏi hiện tại
        public async Task<VoiceAssessmentQuestion> GetCurrentQuestionAsync(Guid assessmentId)
        {
            var assessment = await RestoreAssessmentFromIdAsync(assessmentId);
            if (assessment == null)
                throw new ArgumentException("Bài đánh giá không tồn tại hoặc đã hết hạn.");

            if (assessment.CurrentQuestionIndex >= assessment.Questions.Count)
                throw new ArgumentException("Đã hoàn thành tất cả câu hỏi.");

            return assessment.Questions[assessment.CurrentQuestionIndex];
        }

        // 3. Nộp câu trả lời
        public async Task SubmitVoiceResponseAsync(Guid assessmentId, VoiceAssessmentResponseDto response)
        {
            var assessment = await RestoreAssessmentFromIdAsync(assessmentId);
            if (assessment == null)
                throw new ArgumentException("Bài đánh giá không tồn tại.");

            var question = assessment.Questions.FirstOrDefault(q => q.QuestionNumber == response.QuestionNumber);
            if (question == null)
                throw new ArgumentException("Câu hỏi không tồn tại.");

            question.IsSkipped = response.IsSkipped;

            if (!response.IsSkipped && response.AudioFile != null)
            {
                question.AudioFilePath = await SaveAudioFileAsync(response.AudioFile, assessmentId, response.QuestionNumber);
            }

            assessment.CurrentQuestionIndex++;
            await _redisService.SetVoiceAssessmentAsync(assessment);
        }
        /// <summary>
        /// 4. Hoàn thành bài đánh giá (Đánh giá AI, Gợi ý khóa học)
        /// </summary>
        public async Task<VoiceAssessmentResultDto> CompleteProgramAssessmentAsync(Guid assessmentId)
        {
            _logger.LogInformation("Hoàn thành Program Assessment {AssessmentId}", assessmentId);

            var assessment = await RestoreAssessmentFromIdAsync(assessmentId);
            if (assessment == null || !assessment.ProgramId.HasValue)
                throw new ArgumentException("Assessment không tồn tại hoặc không phải là Program Assessment.");

            var language = await _unitOfWork.Languages.GetByIdAsync(assessment.LanguageId);
            if (language == null)
                throw new ArgumentException("Ngôn ngữ không tồn tại.");

       
            BatchVoiceEvaluationResult evaluationResult;
            var completedQuestions = assessment.Questions.Where(q => !q.IsSkipped).ToList();

            if (completedQuestions.Count == 0)
            {
            
                _logger.LogWarning("Tất cả câu hỏi đã bị bỏ qua. Gán level thấp nhất.");

              
                var allLevels = await _unitOfWork.Levels.GetAllAsync();
                var lowestLevel = allLevels
                    .Where(l => l.ProgramId == assessment.ProgramId.Value)
                    .OrderBy(l => l.OrderIndex)
                    .FirstOrDefault();

               
                string defaultLevel = lowestLevel?.Name ?? "Beginner";

                evaluationResult = new BatchVoiceEvaluationResult
                {
                    OverallLevel = defaultLevel, 
                    OverallScore = 0, 
                    QuestionResults = assessment.Questions.Select(q => new QuestionEvaluationResult
                    {
                        QuestionNumber = q.QuestionNumber,
                        Feedback = "Đã bỏ qua",
                        AccuracyScore = 0,
                        PronunciationScore = 0,
                        FluencyScore = 0,
                        GrammarScore = 0
                    }).ToList(),
                    Strengths = new List<string>(),
                    Weaknesses = new List<string> { "Bạn đã bỏ qua tất cả câu hỏi.", "Hãy thử lại để có kết quả chính xác." },
                    RecommendedCourses = new List<CourseRecommendation>(),
                    EvaluatedAt = DateTime.UtcNow
                };
            }
            else
            {
               
                _logger.LogInformation("Đang gửi {Count} câu trả lời cho AI...", completedQuestions.Count);
                evaluationResult = await _geminiService.EvaluateBatchVoiceResponsesAsync(
                    assessment.Questions,
                    language.LanguageCode,
                    language.LanguageName,
                    assessment.ProgramLevelNames 
                );
            }
           

            var determinedLevel = evaluationResult.OverallLevel;

          
            var learnerLanguage = await GetOrCreateLearnerLanguageAsync(assessment.UserId, assessment.LanguageId);

          
            var recommendedCourses = await GetProgramRecommendationsAsync(
                assessment.ProgramId.Value,
                determinedLevel
            );

        
            var resultDto = new VoiceAssessmentResultDto
            {
                AssessmentId = assessmentId,
                LanguageName = language.LanguageName,
                LaguageID = language.LanguageID,
                LearnerLanguageId = learnerLanguage.LearnerLanguageId,
                ProgramId = assessment.ProgramId,
                ProgramName = assessment.ProgramName,
                DeterminedLevel = determinedLevel,
                OverallScore = evaluationResult.OverallScore,
                PronunciationScore = (int)(evaluationResult.QuestionResults.Any() ? evaluationResult.QuestionResults.Average(q => q.PronunciationScore) : 0),
                FluencyScore = (int)(evaluationResult.QuestionResults.Any() ? evaluationResult.QuestionResults.Average(q => q.FluencyScore) : 0),
                GrammarScore = (int)(evaluationResult.QuestionResults.Any() ? evaluationResult.QuestionResults.Average(q => q.GrammarScore) : 0),
                VocabularyScore = (int)(evaluationResult.QuestionResults.Any() ? evaluationResult.QuestionResults.Average(q => q.AccuracyScore) : 0),
                DetailedFeedback = BuildDetailedFeedback(evaluationResult, assessment),
                KeyStrengths = evaluationResult.Strengths,
                ImprovementAreas = evaluationResult.Weaknesses,
                RecommendedCourses = recommendedCourses.Select(rc => new RecommendedCourseDto
                {
                    CourseId = rc.CourseID,
                    CourseName = rc.CourseName,
                    Level = rc.Level,
                    MatchReason = rc.MatchReason
                }).ToList(),
                CompletedAt = TimeHelper.GetVietnamTime()
            };

       
            await _redisService.SetVoiceAssessmentResultAsync(learnerLanguage.LearnerLanguageId, resultDto);
            await _redisService.SetAsync($"voice_assessment_recommended_courses:{learnerLanguage.LearnerLanguageId}", recommendedCourses, TimeSpan.FromHours(24));
            await _redisService.DeleteVoiceAssessmentAsync(assessmentId);
            await CleanupAudioFilesAsync(assessment.Questions);

            return resultDto;
        }

        // 5. Chấp nhận kết quả
        public async Task AcceptAssessmentAsync(Guid learnerLanguageId)
        {
            var learnerLanguage = await _unitOfWork.LearnerLanguages.GetByIdAsync(learnerLanguageId);
            var resultDto = await GetAssessmentResultAsync(learnerLanguageId);

            if (learnerLanguage == null || resultDto == null)
                throw new InvalidOperationException("Không tìm thấy dữ liệu để chấp nhận.");

            _logger.LogInformation("Chấp nhận Assessment: {Id}. Đặt Level = {Level}", learnerLanguageId, resultDto.DeterminedLevel);

            // Chỉ cập nhật Level
            learnerLanguage.ProficiencyLevel = resultDto.DeterminedLevel;
            learnerLanguage.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.LearnerLanguages.Update(learnerLanguage);
            await _unitOfWork.SaveChangesAsync();

            await ClearRedisDataAsync(learnerLanguageId);
        }

        // 6. Từ chối kết quả
        public async Task RejectAssessmentAsync(Guid learnerLanguageId)
        {
            _logger.LogInformation("Từ chối Assessment: {Id}", learnerLanguageId);
            await ClearRedisDataAsync(learnerLanguageId);
        }

       

        public async Task<VoiceAssessmentResultDto?> GetAssessmentResultAsync(Guid learnerLanguageId)
        {
            return await _redisService.GetVoiceAssessmentResultAsync(learnerLanguageId);
        }

        public async Task<bool> ValidateAssessmentIdAsync(Guid assessmentId, Guid userId)
        {
            var assessment = await RestoreAssessmentFromIdAsync(assessmentId);
            return assessment != null && assessment.UserId == userId;
        }

        public async Task<VoiceAssessmentDto?> RestoreAssessmentFromIdAsync(Guid assessmentId)
        {
            return await _redisService.GetVoiceAssessmentAsync(assessmentId);
        }

        private async Task<LearnerLanguage> GetOrCreateLearnerLanguageAsync(Guid userId, Guid languageId)
        {
            var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
            var learnerLanguage = allLearnerLanguages.FirstOrDefault(ll =>
                ll.UserId == userId &&
                ll.LanguageId == languageId);

            if (learnerLanguage == null)
            {
                learnerLanguage = new LearnerLanguage
                {
                    LearnerLanguageId = Guid.NewGuid(),
                    UserId = userId,
                    LanguageId = languageId,
                    ProficiencyLevel = "Pending Assessment",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.LearnerLanguages.CreateAsync(learnerLanguage);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Đã tạo LearnerLanguage mới: Id={Id}", learnerLanguage.LearnerLanguageId);
            }
            return learnerLanguage;
        }

        private async Task<List<CourseRecommendationDto>> GetProgramRecommendationsAsync(Guid programId, string determinedLevelName)
        {
            try
            {
                var allLevels = await _unitOfWork.Levels.GetAllAsync();
                var programLevels = allLevels.Where(l => l.ProgramId == programId).ToList();

                var determinedLevel = programLevels.FirstOrDefault(l =>
                    string.Equals(l.Name, determinedLevelName, StringComparison.OrdinalIgnoreCase));

                int targetOrderIndex = determinedLevel?.OrderIndex
                    ?? programLevels.Min(l => (int?)l.OrderIndex)
                    ?? 0;

                var targetLevelIds = programLevels
                    .Where(l => l.OrderIndex >= targetOrderIndex)
                    .Select(l => l.LevelId)
                    .ToList();

                var allCourses = await _unitOfWork.Courses.GetAllAsync();
                var courses = allCourses
                    .Where(c => c.ProgramId == programId &&
                                targetLevelIds.Contains(c.LevelId) &&
                                c.Status == CourseStatus.Published)
                    .ToList();

                return courses.Select(c => new CourseRecommendationDto
                {
                    CourseID = c.CourseID,
                    CourseName = c.Title,
                    Level = programLevels.FirstOrDefault(l => l.LevelId == c.LevelId)?.Name ?? "N/A",
                    MatchReason = "Phù hợp với trình độ và khung chương trình của bạn"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy gợi ý khóa học theo Program.");
                return new List<CourseRecommendationDto>();
            }
        }

        private async Task ClearOldAssessmentData(Guid userId, Guid languageId)
        {
            var existingLearnerLang = (await _unitOfWork.LearnerLanguages.GetAllAsync())
                .FirstOrDefault(ll => ll.UserId == userId && ll.LanguageId == languageId);

            if (existingLearnerLang != null)
            {
              
                await ClearRedisDataAsync(existingLearnerLang.LearnerLanguageId);
            }

            var activeAssessments = await _redisService.GetUserAssessmentsAsync(userId, languageId);
            foreach (var assessment in activeAssessments)
            {
                await _redisService.DeleteVoiceAssessmentAsync(assessment.AssessmentId);
            }
        }

        private async Task ClearRedisDataAsync(Guid learnerLanguageId)
        {
            await _redisService.DeleteVoiceAssessmentResultAsync(learnerLanguageId);
            await _redisService.DeleteAsync($"voice_assessment_recommended_courses:{learnerLanguageId}");
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
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xóa file audio: {Path}", question.AudioFilePath);
                }
            }
        }

        private string BuildDetailedFeedback(BatchVoiceEvaluationResult result, VoiceAssessmentDto assessment)
        {
            return $"Bạn đạt cấp độ {result.OverallLevel} với điểm tổng thể {result.OverallScore}/100.";
        }
    }
}