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
        private readonly BLL.Services.AI.ITranscriptionService _stt;

        public VoiceAssessmentService(
            IUnitOfWork unitOfWork,
            IGeminiService geminiService,
            IRedisService redisService,
            ILogger<VoiceAssessmentService> logger,
            BLL.Services.AI.ITranscriptionService stt)
        {
            _unitOfWork = unitOfWork;
            _geminiService = geminiService;
            _redisService = redisService;
            _logger = logger;
            _stt = stt;
        }

        //1. Bắt đầu bài đánh giá (ưu tiên nhanh: timeout AI2s rồi fallback)
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
        

            await ClearOldAssessmentData(userId, languageId);

            List<VoiceAssessmentQuestion>? questions = null;
            try
            {
           
                var aiTask = _geminiService.GenerateVoiceAssessmentQuestionsAsync(
                    language.LanguageCode,
                    language.LanguageName,
                    program.Name
                );
                var completed = await Task.WhenAny(aiTask, Task.Delay(TimeSpan.FromSeconds(2)));
                if (completed == aiTask)
                {
                    questions = aiTask.Result;
                }
                else
                {
                    _logger.LogWarning("Tạo câu hỏi AI quá2s, dùng fallback ngay");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI generate questions failed, will fallback to defaults");
            }

            if (questions == null || !questions.Any())
            {
                questions = new List<VoiceAssessmentQuestion>
                {
                    new() { QuestionNumber =1, Question = $"Introduce yourself in {language.LanguageName}", PromptText = "Tell me your name and hobbies.", VietnameseTranslation = "Giới thiệu tên và sở thích.", MaxRecordingSeconds =30 },
                    new() { QuestionNumber =2, Question = $"Daily routine in {language.LanguageName}", PromptText = "Describe your daily routine.", VietnameseTranslation = "Miêu tả lịch trình mỗi ngày.", MaxRecordingSeconds =60 },
                    new() { QuestionNumber =3, Question = $"Recent experience in {language.LanguageName}", PromptText = "Talk about a recent experience.", VietnameseTranslation = "Kể về trải nghiệm gần đây.", MaxRecordingSeconds =90 },
                    new() { QuestionNumber =4, Question = $"Give an opinion in {language.LanguageName}", PromptText = "Express your opinion about technology.", VietnameseTranslation = "Nêu ý kiến về công nghệ.", MaxRecordingSeconds =120 }
                };
            }

           
          

            var assessment = new VoiceAssessmentDto
            {
                AssessmentId = Guid.NewGuid(),
                UserId = userId,
                LanguageId = languageId,
                LanguageName = language.LanguageName,
                Questions = questions,
                CreatedAt = DateTime.UtcNow,
                CurrentQuestionIndex =0,
                ProgramId = programId,
                ProgramName = program.Name,
            
              
            };

            await _redisService.SetVoiceAssessmentAsync(assessment);
            return assessment;
        }

        public async Task<VoiceAssessmentQuestion> GetCurrentQuestionAsync(Guid assessmentId)
        {
            var assessment = await RestoreAssessmentFromIdAsync(assessmentId);
            if (assessment == null)
                throw new ArgumentException("Bài đánh giá không tồn tại hoặc đã hết hạn.");

            if (assessment.CurrentQuestionIndex >= assessment.Questions.Count)
                throw new ArgumentException("Đã hoàn thành tất cả câu hỏi.");

            return assessment.Questions[assessment.CurrentQuestionIndex];
        }

        //3. Nộp câu trả lời
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
                try
                {
                    question.AudioFilePath = await SaveAudioFileAsync(response.AudioFile, assessmentId, response.QuestionNumber);
                    question.AudioMimeType = response.AudioFile.ContentType; // preserve mime type for AI
                    await using var ms = new MemoryStream();
                    await response.AudioFile.CopyToAsync(ms);
                    var audioBytes = ms.ToArray();
                    var transcript = await _stt.TranscribeAsync(audioBytes, response.AudioFile.FileName, response.AudioFile.ContentType, null);
                    question.Transcript = transcript;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi lưu/nhận dạng audio, sẽ bỏ qua audio cho câu hỏi {Q}", response.QuestionNumber);
                    question.AudioFilePath = null;
                    question.Transcript = null;
                }
            }

            assessment.CurrentQuestionIndex = Math.Max(assessment.CurrentQuestionIndex, response.QuestionNumber);
            await _redisService.SetVoiceAssessmentAsync(assessment);
        }
        /// <summary>
        ///4. Hoàn thành bài đánh giá (Đánh giá AI, Gợi ý khóa học)
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

            // Count completed by either audio or transcript
            var completedQuestions = assessment.Questions
                .Where(q => !q.IsSkipped && (!string.IsNullOrEmpty(q.AudioFilePath) || !string.IsNullOrWhiteSpace(q.Transcript)))
                .ToList();

            BatchVoiceEvaluationResult evaluationResult;
            if (completedQuestions.Count ==0)
            {
                _logger.LogWarning("Không có dữ liệu hợp lệ (audio/transcript). Trả fallback có level & điểm.");
                var defaultLevel = assessment.ProgramLevelNames.FirstOrDefault() ?? "Beginner";
                evaluationResult = new BatchVoiceEvaluationResult
                {
                    OverallLevel = defaultLevel,
                    OverallScore =60,
                    QuestionResults = assessment.Questions.Select(q => new QuestionEvaluationResult
                    {
                        QuestionNumber = q.QuestionNumber,
                        Feedback = q.IsSkipped ? "Đã bỏ qua" : "Không có đủ dữ liệu để chấm.",
                        AccuracyScore =60,
                        PronunciationScore =60,
                        FluencyScore =60,
                        GrammarScore =60
                    }).ToList(),
                    Strengths = new List<string> { "Hoàn thành bài đánh giá" },
                    Weaknesses = new List<string> { "Thiếu dữ liệu audio hoặc transcript" },
                    RecommendedCourses = new List<CourseRecommendation>(),
                    EvaluatedAt = DateTime.UtcNow
                };
            }
            else
            {
                try
                {
                    _logger.LogInformation("Đang gửi {Count} câu trả lời cho AI...", completedQuestions.Count);
                    evaluationResult = await _geminiService.EvaluateBatchVoiceResponsesAsync(
                        assessment.Questions, // pass all so skipped are noted in prompt
                        language.LanguageCode,
                        language.LanguageName,
                        assessment.ProgramLevelNames
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI evaluation failed, fallback kết quả hợp lý");
                    var defaultLevel = assessment.ProgramLevelNames.FirstOrDefault() ?? "Beginner";
                    evaluationResult = new BatchVoiceEvaluationResult
                    {
                        OverallLevel = defaultLevel,
                        OverallScore =65,
                        QuestionResults = assessment.Questions.Select(q => new QuestionEvaluationResult
                        {
                            QuestionNumber = q.QuestionNumber,
                            Feedback = q.IsSkipped ? "Đã bỏ qua" : "Cần cải thiện phát âm và lưu loát.",
                            AccuracyScore =65,
                            PronunciationScore =65,
                            FluencyScore =65,
                            GrammarScore =65
                        }).ToList(),
                        Strengths = new List<string> { "Tham gia đầy đủ" },
                        Weaknesses = new List<string> { "Thiếu đánh giá chi tiết từ AI" },
                        RecommendedCourses = new List<CourseRecommendation>(),
                        EvaluatedAt = DateTime.UtcNow
                    };
                }
            }

            // Ensure OverallLevel not empty
            if (string.IsNullOrWhiteSpace(evaluationResult.OverallLevel))
            {
                evaluationResult.OverallLevel = assessment.ProgramLevelNames.FirstOrDefault() ?? "Beginner";
            }

            var determinedLevel = evaluationResult.OverallLevel;

            var learnerLanguage = await GetOrCreateLearnerLanguageAsync(assessment.UserId, assessment.LanguageId);

            var recommendedCourses = await GetProgramRecommendationsAsync(
                assessment.ProgramId.Value,
                determinedLevel
            );

            // Compute section scores safely
            int avgPron = evaluationResult.QuestionResults.Any() ? (int)evaluationResult.QuestionResults.Average(q => q.PronunciationScore) : evaluationResult.OverallScore;
            int avgFlu = evaluationResult.QuestionResults.Any() ? (int)evaluationResult.QuestionResults.Average(q => q.FluencyScore) : evaluationResult.OverallScore;
            int avgGra = evaluationResult.QuestionResults.Any() ? (int)evaluationResult.QuestionResults.Average(q => q.GrammarScore) : evaluationResult.OverallScore;
            int avgAcc = evaluationResult.QuestionResults.Any() ? (int)evaluationResult.QuestionResults.Average(q => q.AccuracyScore) : evaluationResult.OverallScore;

            var completeness = $"{completedQuestions.Count}/{assessment.Questions.Count}";
            var confidence = completedQuestions.Count >= assessment.Questions.Count -1 ?85 : (completedQuestions.Count >=2 ?70 :55);

            var resultDto = new VoiceAssessmentResultDto
            {
                AssessmentId = assessmentId,
                LanguageName = language.LanguageName,
                LaguageID = language.LanguageID,
                LearnerLanguageId = learnerLanguage.LearnerLanguageId,
                ProgramId = assessment.ProgramId,
                ProgramName = assessment.ProgramName,
                DeterminedLevel = determinedLevel,
               
                AssessmentCompleteness = completeness,
                OverallScore = Math.Max(1, evaluationResult.OverallScore),
                PronunciationScore = Math.Max(1, avgPron),
                FluencyScore = Math.Max(1, avgFlu),
                GrammarScore = Math.Max(1, avgGra),
                VocabularyScore = Math.Max(1, avgAcc),
              
                KeyStrengths = evaluationResult.Strengths?.Any() == true ? evaluationResult.Strengths : new List<string> { "Có động lực tham gia", "Cố gắng hoàn thành bài" },
                ImprovementAreas = evaluationResult.Weaknesses?.Any() == true ? evaluationResult.Weaknesses : new List<string> { "Tăng thời lượng luyện nói", "Cải thiện phát âm và ngữ điệu" },
                NextLevelRequirements = "Hoàn thành thêm các bài luyện nói và cải thiện phát âm, ngữ pháp cơ bản.",
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

    

   
        public async Task<VoiceAssessmentResultDto?> GetAssessmentResultAsync(Guid learnerLanguageId)
        {
            return await _redisService.GetVoiceAssessmentResultAsync(learnerLanguageId);
        }

        public async Task AcceptAssessmentAsync(Guid learnerLanguageId)
        {
            var learnerLanguage = await _unitOfWork.LearnerLanguages.GetByIdAsync(learnerLanguageId);
            var resultDto = await GetAssessmentResultAsync(learnerLanguageId);

            if (learnerLanguage == null || resultDto == null)
                throw new InvalidOperationException("Không tìm thấy dữ liệu để chấp nhận.");

            _logger.LogInformation("Chấp nhận Assessment: {Id}. Đặt Level = {Level}", learnerLanguageId, resultDto.DeterminedLevel);

            learnerLanguage.ProficiencyLevel = resultDto.DeterminedLevel;
            learnerLanguage.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.LearnerLanguages.Update(learnerLanguage);
            await _unitOfWork.SaveChangesAsync();

            await ClearRedisDataAsync(learnerLanguageId);
        }

        public async Task RejectAssessmentAsync(Guid learnerLanguageId)
        {
            _logger.LogInformation("Từ chối Assessment: {Id}", learnerLanguageId);
            await ClearRedisDataAsync(learnerLanguageId);
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

        private async Task<string> SaveAudioFileAsync(IFormFile audioFile, Guid assessmentId, int questionNumber)
        {
            var root = Environment.GetEnvironmentVariable("UPLOAD_ROOT");
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Path.GetTempPath(), "flearn-uploads");
            }
            var uploadsFolder = Path.Combine(root, "voice-assessments");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(audioFile.FileName);
            var fileName = $"{assessmentId}_{questionNumber}_{Guid.NewGuid()}{extension}";
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
                    ??0;

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

        private string BuildDetailedFeedback(BatchVoiceEvaluationResult result, VoiceAssessmentDto assessment)
        {
            return $"Bạn đạt cấp độ {result.OverallLevel} với điểm tổng thể {Math.Max(1, result.OverallScore)}/100.";
        }
    }
}