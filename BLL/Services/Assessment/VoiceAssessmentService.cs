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
using System.Security.Cryptography;
using System.Text;

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

        //1. Bắt đầu bài đánh giá (ưu tiên nhanh: timeout AI6s rồi fallback + cache Redis)
        public async Task<VoiceAssessmentDto> StartProgramAssessmentAsync(Guid userId, Guid languageId, Guid programId)
        {
            _logger.LogInformation("Bắt đầu Program Assessment: User {UserId}, Program {ProgramId}", userId, programId);

            var program = await _unitOfWork.Programs.GetByIdAsync(programId);
            if (program == null || program.LanguageId != languageId)
                throw new ArgumentException("Khung chương trình không hợp lệ.");

            var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
            if (language == null)
                throw new ArgumentException("Ngôn ngữ không tồn tại.");

            // Load program levels in ascending order
            var allLevels = await _unitOfWork.Levels.GetAllAsync();
            var programLevelNames = allLevels
                .Where(l => l.ProgramId == programId)
                .OrderBy(l => l.OrderIndex)
                .Select(l => l.Name)
                .ToList();
            if (!programLevelNames.Any())
            {
                _logger.LogWarning("Program {ProgramId} chưa có levels, dùng thang mặc định theo ngôn ngữ", programId);
            }

            await ClearOldAssessmentData(userId, languageId);

            // Cache theo languageId+programId+version(hash levels)
            var levelsVersion = ComputeLevelsHash(programLevelNames);
            var cacheKey = $"voice_questions:{languageId}:{programId}:{levelsVersion}";
            List<VoiceAssessmentQuestion>? questions = await _redisService.GetAsync<List<VoiceAssessmentQuestion>>(cacheKey);
            if (questions != null && questions.Any())
            {
                _logger.LogInformation("Hit cache câu hỏi: {Key}", cacheKey);
            }
            else
            {
                try
                {
                    // Wait up to6s so user can see AI-generated questions
                    var aiTask = _geminiService.GenerateVoiceAssessmentQuestionsAsync(
                        language.LanguageCode,
                        language.LanguageName,
                        program.Name
                    );
                    var completed = await Task.WhenAny(aiTask, Task.Delay(TimeSpan.FromSeconds(20)));
                    if (completed == aiTask)
                    {
                        questions = await aiTask;
                    }
                    else
                    {
                        _logger.LogWarning("Tạo câu hỏi AI quá6s, dùng fallback ngay");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI generate questions failed, will fallback to defaults");
                }

                if (questions == null || !questions.Any())
                {
                    questions = BuildLocalizedFallbackQuestions(language.LanguageCode, language.LanguageName, programLevelNames);
                }

                // Chuẩn hóa và sắp xếp tăng dần
                questions = questions
                    .OrderBy(q => MapDifficultyOrder(q.Difficulty, programLevelNames))
                    .Select((q, idx) => { q.QuestionNumber = idx +1; return q; })
                    .ToList();

                // Lưu cache9 giờ
                await _redisService.SetAsync(cacheKey, questions, TimeSpan.FromHours(9));
            }

            var assessment = new VoiceAssessmentDto
            {
                AssessmentId = Guid.NewGuid(),
                UserId = userId,
                LanguageId = languageId,
                LanguageName = language.LanguageName,
                Questions = questions!,
                CreatedAt = TimeHelper.GetVietnamTime(),
                CurrentQuestionIndex =0,
                ProgramId = programId,
                ProgramName = program.Name,
                ProgramLevelNames = programLevelNames
            };

            await _redisService.SetVoiceAssessmentAsync(assessment);
            return assessment;
        }

        private static string ComputeLevelsHash(List<string> programLevelNames)
        {
            var input = string.Join('|', programLevelNames.Select(s => s.Trim()));
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).Substring(0,12);
        }

        private List<VoiceAssessmentQuestion> BuildLocalizedFallbackQuestions(string languageCode, string languageName, List<string> programLevelNames)
        {
            var diffs = programLevelNames.Count >=4 ? programLevelNames.Take(4).ToArray() : BuildDefaultDifficulties(languageCode);
            var code = (languageCode ?? string.Empty).ToLowerInvariant();
            if (code.StartsWith("ja"))
            {
                return new List<VoiceAssessmentQuestion>
                {
                    new() { QuestionNumber =1, Question = "自己紹介", PromptText = "あなたの名前と趣味を紹介してください。", VietnameseTranslation = "Giới thiệu tên và sở thích.", QuestionType = "speaking", Difficulty = diffs[0], MaxRecordingSeconds =30 },
                    new() { QuestionNumber =2, Question = "一日の流れ", PromptText = "朝から夜までの一日の過ごし方を説明してください。", VietnameseTranslation = "Miêu tả lịch trình mỗi ngày.", QuestionType = "speaking", Difficulty = diffs[1], MaxRecordingSeconds =60 },
                    new() { QuestionNumber =3, Question = "最近の経験", PromptText = "最近の出来事について話してください。", VietnameseTranslation = "Kể về trải nghiệm gần đây.", QuestionType = "speaking", Difficulty = diffs[2], MaxRecordingSeconds =90 },
                    new() { QuestionNumber =4, Question = "意見を述べる", PromptText = "テクノロジーについてあなたの意見を述べてください。", VietnameseTranslation = "Nêu ý kiến về công nghệ.", QuestionType = "speaking", Difficulty = diffs[3], MaxRecordingSeconds =120 }
                };
            }
            if (code.StartsWith("zh"))
            {
                return new List<VoiceAssessmentQuestion>
                {
                    new() { QuestionNumber =1, Question = "自我介绍", PromptText = "请介绍你的名字和爱好。", VietnameseTranslation = "Giới thiệu tên và sở thích.", QuestionType = "speaking", Difficulty = diffs[0], MaxRecordingSeconds =30 },
                    new() { QuestionNumber =2, Question = "日常安排", PromptText = "请描述你每天的作息。", VietnameseTranslation = "Miêu tả lịch trình mỗi ngày.", QuestionType = "speaking", Difficulty = diffs[1], MaxRecordingSeconds =60 },
                    new() { QuestionNumber =3, Question = "最近经历", PromptText = "请谈谈你最近的一次经历。", VietnameseTranslation = "Kể về trải nghiệm gần đây.", QuestionType = "speaking", Difficulty = diffs[2], MaxRecordingSeconds =90 },
                    new() { QuestionNumber =4, Question = "表达观点", PromptText = "请谈谈你对科技的看法。", VietnameseTranslation = "Nêu ý kiến về công nghệ.", QuestionType = "speaking", Difficulty = diffs[3], MaxRecordingSeconds =120 }
                };
            }
            // default (e.g., English)
            return new List<VoiceAssessmentQuestion>
            {
                new() { QuestionNumber =1, Question = $"Introduce yourself in {languageName}", PromptText = "Tell me your name and hobbies.", VietnameseTranslation = "Giới thiệu tên và sở thích.", QuestionType = "speaking", Difficulty = diffs[0], MaxRecordingSeconds =30 },
                new() { QuestionNumber =2, Question = $"Daily routine in {languageName}", PromptText = "Describe your daily routine.", VietnameseTranslation = "Miêu tả lịch trình mỗi ngày.", QuestionType = "speaking", Difficulty = diffs[1], MaxRecordingSeconds =60 },
                new() { QuestionNumber =3, Question = $"Recent experience in {languageName}", PromptText = "Talk about a recent experience.", VietnameseTranslation = "Kể về trải nghiệm gần đây.", QuestionType = "speaking", Difficulty = diffs[2], MaxRecordingSeconds =90 },
                new() { QuestionNumber =4, Question = $"Give an opinion in {languageName}", PromptText = "Express your opinion about technology.", VietnameseTranslation = "Nêu ý kiến về công nghệ.", QuestionType = "speaking", Difficulty = diffs[3], MaxRecordingSeconds =120 }
            };
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
                    // Save audio
                    question.AudioFilePath = await SaveAudioFileAsync(response.AudioFile, assessmentId, response.QuestionNumber);
                    question.AudioMimeType = response.AudioFile.ContentType; // preserve mime type for AI

                    await using var ms = new MemoryStream();
                    await response.AudioFile.CopyToAsync(ms);
                    var audioBytes = ms.ToArray();

                    // Determine explicit language code for STT to avoid auto-detect issues
                    var lang = await _unitOfWork.Languages.GetByIdAsync(assessment.LanguageId);
                    var langCode = NormalizeToBcp47(lang?.LanguageCode);

                    // STT with explicit language
                    var transcript = await _stt.TranscribeAsync(audioBytes, response.AudioFile.FileName, response.AudioFile.ContentType, langCode);
                    question.Transcript = transcript;

                    // Log summary for this question
                    _logger.LogInformation(
                        "VoiceAssessment Submit: Assessment={AssessmentId}, Q={QNumber}, AudioSaved={HasAudio}, Path='{Path}', Size={Size}B, Mime='{Mime}', TranscriptLen={TLen}, TranscriptPreview='{TPrev}'",
                        assessmentId,
                        response.QuestionNumber,
                        !string.IsNullOrEmpty(question.AudioFilePath),
                        question.AudioFilePath,
                        response.AudioFile.Length,
                        response.AudioFile.ContentType,
                        question.Transcript?.Length ??0,
                        Trunc(question.Transcript,120)
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Lỗi lưu/nhận dạng audio cho Assessment={AssessmentId}, Q={QNumber}. Sẽ bỏ qua audio/transcript.",
                        assessmentId,
                        response.QuestionNumber
                    );
                    question.AudioFilePath = null;
                    question.Transcript = null;
                }
            }
            else
            {
                _logger.LogInformation(
                    "VoiceAssessment Submit: Assessment={AssessmentId}, Q={QNumber}, Skipped={Skipped}, HasAudio={HasAudio}",
                    assessmentId,
                    response.QuestionNumber,
                    response.IsSkipped,
                    response.AudioFile != null
                );
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

            // Log inputs summary
            LogQuestionInputSummary(assessment.Questions, completedQuestions);

            BatchVoiceEvaluationResult evaluationResult;
            if (completedQuestions.Count == 0)
            {
                _logger.LogWarning("Không có dữ liệu hợp lệ (audio/transcript). Trả fallback có level & điểm.");
                var defaultLevel = assessment.ProgramLevelNames.FirstOrDefault() ?? "Beginner";
                evaluationResult = new BatchVoiceEvaluationResult
                {
                    OverallLevel = defaultLevel,
                    OverallScore = 60,
                    QuestionResults = assessment.Questions.Select(q => new QuestionEvaluationResult
                    {
                        QuestionNumber = q.QuestionNumber,
                        Feedback = q.IsSkipped ? "Đã bỏ qua" : "Không có đủ dữ liệu để chấm.",
                        AccuracyScore = 60,
                        PronunciationScore = 60,
                        FluencyScore = 60,
                        GrammarScore = 60
                    }).ToList(),
                    Strengths = new List<string> { "Hoàn thành bài đánh giá" },
                    Weaknesses = new List<string> { "Thiếu dữ liệu audio hoặc transcript" },
                    RecommendedCourses = new List<CourseRecommendation>(),
                    EvaluatedAt = TimeHelper.GetVietnamTime()
                };
            }
            else
            {
                try
                {
                    _logger.LogInformation(
                        "Đang gửi {Count} câu trả lời cho AI... (LanguageCode={LangCode}, ProgramLevels=[{Levels}])",
                        completedQuestions.Count,
                        language.LanguageCode,
                        string.Join(", ", assessment.ProgramLevelNames ?? new List<string>())
                    );

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    // IMPORTANT: chỉ gửi những câu có dữ liệu hợp lệ cho AI (bug fix)
                    evaluationResult = await _geminiService.EvaluateBatchVoiceResponsesAsync(
                        completedQuestions,
                        language.LanguageCode,
                        language.LanguageName,
                        assessment.ProgramLevelNames
                    );

                    sw.Stop();
                    _logger.LogInformation("AI đánh giá xong trong {ElapsedMs}ms", sw.ElapsedMilliseconds);

                    // Log AI output summary
                    LogEvaluationSummary(evaluationResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI evaluation failed, fallback kết quả hợp lý");
                    var defaultLevel = assessment.ProgramLevelNames.FirstOrDefault() ?? "Beginner";
                    evaluationResult = new BatchVoiceEvaluationResult
                    {
                        OverallLevel = defaultLevel,
                        OverallScore = 65,
                        QuestionResults = assessment.Questions.Select(q => new QuestionEvaluationResult
                        {
                            QuestionNumber = q.QuestionNumber,
                            Feedback = q.IsSkipped ? "Đã bỏ qua" : "Cần cải thiện phát âm và lưu loát.",
                            AccuracyScore = 65,
                            PronunciationScore = 65,
                            FluencyScore = 65,
                            GrammarScore = 65
                        }).ToList(),
                        Strengths = new List<string> { "Tham gia đầy đủ" },
                        Weaknesses = new List<string> { "Thiếu đánh giá chi tiết từ AI" },
                        RecommendedCourses = new List<CourseRecommendation>(),
                        EvaluatedAt = TimeHelper.GetVietnamTime()
                    };
                }
            }

            // Ensure OverallLevel not empty
            if (string.IsNullOrWhiteSpace(evaluationResult.OverallLevel))
            {
                var fallbackLevel = assessment.ProgramLevelNames.FirstOrDefault() ?? "Beginner";
                _logger.LogWarning("AI không trả OverallLevel. Dùng fallback: {FallbackLevel}", fallbackLevel);
                evaluationResult.OverallLevel = fallbackLevel;
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
            var confidence = completedQuestions.Count >= assessment.Questions.Count - 1 ? 85 : (completedQuestions.Count >= 2 ? 70 : 55);

            var resultDto = new VoiceAssessmentResultDto
            {
                AssessmentId = assessmentId,
                LanguageName = language.LanguageName,
                LaguageID = language.LanguageID,
                LearnerLanguageId = learnerLanguage.LearnerLanguageId,
                ProgramId = assessment.ProgramId,
                ProgramName = assessment.ProgramName,
                DeterminedLevel = determinedLevel,
                LevelConfidence = confidence,
                AssessmentCompleteness = completeness,
                OverallScore = Math.Max(1, evaluationResult.OverallScore),
                PronunciationScore = Math.Max(1, avgPron),
                FluencyScore = Math.Max(1, avgFlu),
                GrammarScore = Math.Max(1, avgGra),
                VocabularyScore = Math.Max(1, avgAcc),
                DetailedFeedback = BuildDetailedFeedback(evaluationResult, assessment),
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

            _logger.LogInformation(
                "Assessment Result: Assessment={AssessmentId}, OverallLevel={Level}, OverallScore={Score}, Completeness={Completeness}, Confidence={Confidence}",
                assessmentId,
                resultDto.DeterminedLevel,
                resultDto.OverallScore,
                resultDto.AssessmentCompleteness,
                resultDto.LevelConfidence
            );

            await _redisService.SetVoiceAssessmentResultAsync(learnerLanguage.LearnerLanguageId, resultDto);
            await _redisService.SetAsync($"voice_assessment_recommended_courses:{learnerLanguage.LearnerLanguageId}", recommendedCourses, TimeSpan.FromHours(24));
            await _redisService.DeleteVoiceAssessmentAsync(assessmentId);
            await CleanupAudioFilesAsync(assessment.Questions);

            return resultDto;
        }

        private int MapDifficultyOrder(string difficulty, List<string> programLevelNames)
        {
            if (string.IsNullOrWhiteSpace(difficulty)) return int.MaxValue;
            var index = programLevelNames.FindIndex(l => l.Equals(difficulty, StringComparison.OrdinalIgnoreCase));
            if (index >=0) return index;
            var d = difficulty.Trim().ToUpperInvariant();
            if (d.StartsWith("HSK"))
            {
                var numStr = new string(d.Skip(3).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(numStr, out var n))
                    return Math.Clamp(n -1,0,5);
            }
            if (d.Length >=2 && d[0] == 'N' && char.IsDigit(d[1]))
            {
                return d[1] switch { '5' =>0, '4' =>1, '3' =>2, '2' =>3, '1' =>4, _ => int.MaxValue };
            }
            return d switch
            {
                "A1" =>0,
                "A2" =>1,
                "B1" =>2,
                "B2" =>3,
                "C1" =>4,
                "C2" =>5,
                _ => int.MaxValue
            };
        }

        private static string[] BuildDefaultDifficulties(string languageCode)
        {
            var code = (languageCode ?? string.Empty).Trim().ToLowerInvariant();
            if (code.StartsWith("zh")) return new[] { "HSK1", "HSK2", "HSK3", "HSK4" };
            if (code.StartsWith("ja")) return new[] { "N5", "N4", "N3", "N2" };
            return new[] { "A1", "A2", "B1", "B2" };
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
            learnerLanguage.UpdatedAt = TimeHelper.GetVietnamTime();

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
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
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

        // Helpers for logging
        private void LogQuestionInputSummary(List<VoiceAssessmentQuestion> allQuestions, List<VoiceAssessmentQuestion> completedQuestions)
        {
            try
            {
                _logger.LogInformation(
                    "VoiceAssessment Summary: TotalQ={Total}, CompletedQ={Completed}",
                    allQuestions?.Count ?? 0,
                    completedQuestions?.Count ?? 0
                );

                foreach (var q in allQuestions.OrderBy(q => q.QuestionNumber))
                {
                    _logger.LogInformation(
                        "Q{Q}: Skipped={Skipped}, HasAudio={HasAudio}, Mime='{Mime}', TranscriptLen={TLen}, Diff='{Diff}', TranscriptPreview='{Preview}'",
                        q.QuestionNumber,
                        q.IsSkipped,
                        !string.IsNullOrEmpty(q.AudioFilePath),
                        q.AudioMimeType,
                        q.Transcript?.Length ?? 0,
                        q.Difficulty,
                        Trunc(q.Transcript, 120)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể log tóm tắt câu hỏi.");
            }
        }

        private void LogEvaluationSummary(BatchVoiceEvaluationResult evaluation)
        {
            try
            {
                _logger.LogInformation(
                    "AI Output: OverallLevel={Level}, OverallScore={Score}, QCount={QCount}",
                    evaluation.OverallLevel,
                    evaluation.OverallScore,
                    evaluation.QuestionResults?.Count ?? 0
                );

                if (evaluation.QuestionResults != null)
                {
                    foreach (var qr in evaluation.QuestionResults.OrderBy(r => r.QuestionNumber))
                    {
                        _logger.LogInformation(
                            "AI Q{Q}: Acc={Acc}, Pron={Pron}, Flu={Flu}, Gra={Gra}, Feedback='{Feedback}'",
                            qr.QuestionNumber,
                            qr.AccuracyScore,
                            qr.PronunciationScore,
                            qr.FluencyScore,
                            qr.GrammarScore,
                            Trunc(qr.Feedback, 160)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể log tóm tắt kết quả AI.");
            }
        }

        private static string Trunc(string? s, int max = 120)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s[..max] + "…";
        }

        private static string NormalizeToBcp47(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "en-US";
            var c = code.Trim();
            return c.ToLowerInvariant() switch
            {
                "en" => "en-US",
                "ja" => "ja-JP",
                "zh" => "zh-CN",
                _ => c
            };
        }
    }
}