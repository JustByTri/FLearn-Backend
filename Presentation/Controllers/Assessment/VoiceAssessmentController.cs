using BLL.IServices.AI;
using BLL.IServices.Assessment;
using Common.DTO.Assement;
using Common.DTO.Learner;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Linq;
using BLL.IServices.Course;
namespace Presentation.Controllers.Assessment
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VoiceAssessmentController : ControllerBase
    {
        private readonly IVoiceAssessmentService _voiceAssessmentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VoiceAssessmentController> _logger;
        private readonly IGeminiService _geminiService;
        private readonly ICourseRecommendationService _courseRecommendationService;

        public VoiceAssessmentController(
            IVoiceAssessmentService voiceAssessmentService,
            IUnitOfWork unitOfWork,
            ICourseRecommendationService courseRecommendationService,  
            ILogger<VoiceAssessmentController> logger)
        {
            _logger = logger;
            _voiceAssessmentService = voiceAssessmentService;
            _unitOfWork = unitOfWork;
            _courseRecommendationService = courseRecommendationService;  
        }

        /// <summary>
        /// Bắt đầu bài đánh giá giọng nói - TỰ ĐỘNG LƯU VÀO REDIS
        /// </summary>
        /// <summary>
        /// Bắt đầu bài đánh giá giọng nói - TỰ ĐỘNG LƯU VÀO REDIS
        /// </summary>
        [HttpPost("start/{languageId:guid}")]
        public async Task<IActionResult> StartVoiceAssessment(Guid languageId, [FromQuery] int? goalId = null)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Lấy thông tin ngôn ngữ từ DB
                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Ngôn ngữ không tồn tại",
                        errorCode = "LANGUAGE_NOT_FOUND"
                    });
                }

           
                _logger.LogInformation("🔍 Language from DB: Name={Name}, Code=[{Code}], Length={Length}",
                    language.LanguageName,
                    language.LanguageCode,
                    language.LanguageCode?.Length ?? 0);

        
                var trimmedCode = language.LanguageCode?.Trim().ToUpper();

                var supportedLanguages = new[] { "EN", "ZH", "JP" };

                if (string.IsNullOrEmpty(trimmedCode) || !supportedLanguages.Contains(trimmedCode))
                {
                    _logger.LogWarning("❌ Unsupported language code: [{Code}] (trimmed: [{Trimmed}])",
                        language.LanguageCode, trimmedCode);

                    return BadRequest(new
                    {
                        success = false,
                        message = "Chỉ hỗ trợ đánh giá giọng nói tiếng Anh, tiếng Trung và tiếng Nhật",
                        errorCode = "LANGUAGE_NOT_SUPPORTED"
                    });
                }

             
                var assessment = await _voiceAssessmentService.StartVoiceAssessmentAsync(
                    userId, languageId, goalId);

                return Ok(new
                {
                    success = true,
                    message = $"Bắt đầu đánh giá giọng nói {assessment.LanguageName}" +
                             (assessment.GoalName != null ? $" - Mục tiêu: {assessment.GoalName}" : ""),
                    data = new
                    {
                        assessmentId = assessment.AssessmentId,
                        languageName = assessment.LanguageName,
                        goalName = assessment.GoalName,
                        totalQuestions = assessment.Questions.Count,
                        currentQuestionIndex = assessment.CurrentQuestionIndex,
                        firstQuestion = assessment.Questions.FirstOrDefault()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error starting voice assessment");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy câu hỏi hiện tại
        /// </summary>
        [HttpGet("{assessmentId:guid}/current-question")]
        public async Task<IActionResult> GetCurrentQuestion(Guid assessmentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var isValid = await _voiceAssessmentService.ValidateAssessmentIdAsync(assessmentId, userId);
                if (!isValid)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Bạn không có quyền truy cập assessment này",
                        errorCode = "UNAUTHORIZED_ACCESS"
                    });
                }

                var question = await _voiceAssessmentService.GetCurrentQuestionAsync(assessmentId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        questionNumber = question.QuestionNumber,
                        question = question.Question,
                        promptText = question.PromptText,
                        vietnameseTranslation = question.VietnameseTranslation,
                        wordGuides = question.WordGuides,
                        questionType = question.QuestionType,
                        difficulty = question.Difficulty,
                        maxRecordingSeconds = question.MaxRecordingSeconds,
                        canSkip = true
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Submit voice với validation mạnh
        /// </summary>
        [HttpPost("{assessmentId:guid}/submit-voice")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitVoiceResponse(
            [FromRoute] Guid assessmentId,
            [FromForm] VoiceSubmissionFormDto formDto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Validate ownership
                var isValid = await _voiceAssessmentService.ValidateAssessmentIdAsync(assessmentId, userId);
                if (!isValid)
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Bạn không có quyền truy cập assessment này",
                        errorCode = "UNAUTHORIZED_ACCESS"
                    });

                // Validate audio file if not skipped
                if (!formDto.IsSkipped)
                {
                    if (formDto.AudioFile == null)
                        return BadRequest(new { success = false, message = "Cần gửi file âm thanh hoặc chọn bỏ qua" });

                    var allowedTypes = new[] { "audio/mp3", "audio/wav", "audio/m4a", "audio/webm", "audio/mpeg" };
                    if (!allowedTypes.Contains(formDto.AudioFile.ContentType.ToLower()))
                        return BadRequest(new { success = false, message = "Chỉ hỗ trợ MP3, WAV, M4A, WebM" });

                    if (formDto.AudioFile.Length > 10 * 1024 * 1024)
                        return BadRequest(new { success = false, message = "File không được vượt quá 10MB" });
                }

                var response = new VoiceAssessmentResponseDto
                {
                    AssessmentId = assessmentId,
                    QuestionNumber = formDto.QuestionNumber,
                    IsSkipped = formDto.IsSkipped,
                    AudioFile = formDto.AudioFile
                };

                await _voiceAssessmentService.SubmitVoiceResponseAsync(assessmentId, response);

                // Check if completed
                var assessment = await _voiceAssessmentService.RestoreAssessmentFromIdAsync(assessmentId);
                var isCompleted = assessment?.CurrentQuestionIndex >= assessment?.Questions.Count;

                return Ok(new
                {
                    success = true,
                    message = formDto.IsSkipped ? "Đã bỏ qua câu hỏi" : "Đã lưu câu trả lời",
                    data = new
                    {
                        questionNumber = formDto.QuestionNumber,
                        saved = !formDto.IsSkipped,
                        isCompleted = isCompleted,
                        nextQuestionIndex = assessment?.CurrentQuestionIndex
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting voice response");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("smart-start/{languageId:guid}")]
        public async Task<IActionResult> SmartStartVoiceAssessment(Guid languageId, [FromQuery] int? goalId = null)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Ngôn ngữ không tồn tại",
                        errorCode = "LANGUAGE_NOT_FOUND"
                    });
                }

                // Fix: Use lowercase language codes to match your database
                var supportedLanguages = new[] { "en", "zh", "ja" }; // Changed from "EN", "ZH", "JP"
                if (!supportedLanguages.Contains(language.LanguageCode.ToLower())) // Added .ToLower() for safety
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chỉ hỗ trợ đánh giá giọng nói tiếng Anh, tiếng Trung và tiếng Nhật",
                        errorCode = "LANGUAGE_NOT_SUPPORTED"
                    });
                }

                // Xóa assessment cũ trong Redis (nếu có)
                await _voiceAssessmentService.ClearAssessmentResultAsync(userId, languageId);

                // Bắt đầu assessment mới
                var newAssessment = await _voiceAssessmentService.StartVoiceAssessmentAsync(userId, languageId, goalId);

                return Ok(new
                {
                    success = true,
                    message = $"Bắt đầu đánh giá {language.LanguageName}",
                    action = "started",
                    data = new
                    {
                        assessmentId = newAssessment.AssessmentId,
                        languageName = newAssessment.LanguageName,
                        goalName = newAssessment.GoalName,
                        totalQuestions = newAssessment.Questions.Count,
                        currentQuestionIndex = newAssessment.CurrentQuestionIndex,
                        firstQuestion = newAssessment.Questions.FirstOrDefault()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        /// <summary>
        /// Lấy kết quả đánh giá giọng nói đã hoàn thành
        /// </summary>
        [HttpGet("result/{languageId:guid}")]
        public async Task<IActionResult> GetAssessmentResult(Guid languageId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _voiceAssessmentService.GetVoiceAssessmentResultAsync(userId, languageId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Bạn chưa hoàn thành đánh giá giọng nói cho ngôn ngữ này"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy kết quả đánh giá thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra đã hoàn thành đánh giá chưa
        /// </summary>
        [HttpGet("status/{languageId:guid}")]
        public async Task<IActionResult> CheckAssessmentStatus(Guid languageId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var hasCompleted = await _voiceAssessmentService.HasCompletedVoiceAssessmentAsync(userId, languageId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        hasCompleted = hasCompleted,
                        canStartNew = !hasCompleted
                    },
                    message = hasCompleted ? "Đã hoàn thành đánh giá" : "Chưa hoàn thành đánh giá"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Complete voice assessment và lưu kết quả vào Redis để đợi accept/reject
        /// </summary>
        /// <summary>
        /// Complete voice assessment và lưu kết quả vào Redis để đợi accept/reject
        /// </summary>
       [ HttpPost("complete/{assessmentId:guid}")]
public async Task<IActionResult> CompleteVoiceAssessment(Guid assessmentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("User {UserId} completing assessment {AssessmentId}", userId, assessmentId);

             
                var assessment = await _voiceAssessmentService.RestoreAssessmentFromIdAsync(assessmentId);
                if (assessment == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Assessment không tồn tại hoặc đã hết hạn",
                        errorCode = "ASSESSMENT_NOT_FOUND"
                    });
                }

                if (assessment.UserId != userId)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Assessment này không thuộc về bạn",
                        errorCode = "ASSESSMENT_OWNERSHIP_MISMATCH"
                    });
                }

                if (assessment.CurrentQuestionIndex == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn chưa trả lời câu hỏi nào",
                        errorCode = "NO_ANSWERS_SUBMITTED"
                    });
                }

               
                var assessmentResult = await _voiceAssessmentService.CompleteVoiceAssessmentAsync(assessmentId);

              
                LearnerLanguage learnerLanguage;
                try
                {
                    var findResult = await _unitOfWork.LearnerLanguages
                        .FindAsync(ll => ll.UserId == userId && ll.LanguageId == assessment.LanguageId);

                 
                    if (findResult is IEnumerable<LearnerLanguage> enumerable)
                    {
                        learnerLanguage = enumerable.FirstOrDefault();
                    }
                    else
                    {
                        learnerLanguage = findResult as LearnerLanguage;
                    }
                }
                catch
                {
                    
                    var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                    learnerLanguage = allLearnerLanguages
                        .FirstOrDefault(ll => ll.UserId == userId && ll.LanguageId == assessment.LanguageId);
                }

                if (learnerLanguage == null)
                {
                    learnerLanguage = new LearnerLanguage
                    {
                        LearnerLanguageId = Guid.NewGuid(),
                        UserId = userId,
                        LanguageId = assessment.LanguageId,
                        GoalId = assessment.GoalID,
                        ProficiencyLevel = "Pending Assessment",
                        StreakDays = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.LearnerLanguages.CreateAsync(learnerLanguage);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Created new LearnerLanguage {LearnerLanguageId}", learnerLanguage.LearnerLanguageId);
                }

           
                var recommendedCourses = await _courseRecommendationService.GetRecommendedCoursesAsync(
                    assessment.LanguageId,
                    assessmentResult.OverallLevel,
                    assessment.GoalID);

                _logger.LogInformation("Found {Count} recommended courses for level {Level}",
                    recommendedCourses.Count, assessmentResult.OverallLevel);

             
                var hasCoursesForLevel = await _courseRecommendationService.HasCoursesForLevelAsync(
                    assessment.LanguageId,
                    assessmentResult.OverallLevel);

             
                await _voiceAssessmentService.SaveRecommendedCoursesAsync(
                    userId,
                    assessment.LanguageId,
                    recommendedCourses);

           
                var message = hasCoursesForLevel && recommendedCourses.Any()
                    ? $"Hoàn thành voice assessment thành công! Tìm thấy {recommendedCourses.Count} khóa học phù hợp với trình độ {assessmentResult.OverallLevel} của bạn."
                    : $"Hoàn thành voice assessment thành công! Hiện tại chưa có khóa học tương ứng với trình độ {assessmentResult.OverallLevel}. Hãy tham khảo các khóa học khác trong hệ thống.";

                return Ok(new
                {
                    success = true,
                    message = message,
                    data = new
                    {
                        assessmentId = assessmentId,
                        determinedLevel = assessmentResult.OverallLevel,
                        overallScore = assessmentResult.OverallScore,
                        strengths = assessmentResult.Strengths,
                        weaknesses = assessmentResult.Weaknesses,
                        languageId = assessment.LanguageId,
                        languageName = assessment.LanguageName,
                        learnerLanguageId = learnerLanguage.LearnerLanguageId,
                        requiresAcceptance = true,

                     
                        recommendedCourses = recommendedCourses,
                        hasRecommendedCourses = recommendedCourses.Any(),
                        coursesCount = recommendedCourses.Count,
                        hasCoursesForLevel = hasCoursesForLevel
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing voice assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi hoàn thành assessment",
                    error = ex.Message
                });
            }
        }


        /// <summary>
        /// Accept voice assessment result và lưu vào DB (tạo LearnerLanguage, Roadmap, LearnerSlotBalance)
        /// </summary>
        [HttpPost("accept-voice-assessment")]
        public async Task<IActionResult> AcceptVoiceAssessmentResult([FromBody] AcceptVoiceAssessmentRequestDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

               
                var learnerLanguage = await _unitOfWork.LearnerLanguages.GetByIdAsync(request.LearnerLanguageId);
                if (learnerLanguage == null || learnerLanguage.UserId != userId)
                {
                    return BadRequest(new { success = false, message = "LearnerLanguage không tồn tại hoặc không thuộc về bạn" });
                }

             
                var assessmentResult = await _voiceAssessmentService.GetVoiceAssessmentResultAsync(userId, learnerLanguage.LanguageId);
                if (assessmentResult == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy kết quả assessment trong Redis" });
                }

           
                await SaveAcceptedAssessmentToDatabase(learnerLanguage, assessmentResult);

                await _voiceAssessmentService.ClearAssessmentResultAsync(userId, learnerLanguage.LanguageId);

                return Ok(new
                {
                    success = true,
                    message = "Đã chấp nhận và lưu kết quả voice assessment thành công! Roadmap và slot balance đã được tạo.",
                    data = new
                    {
                        learnerLanguageId = request.LearnerLanguageId,
                        determinedLevel = assessmentResult.DeterminedLevel,
                        roadmapCreated = true,
                        slotBalanceCreated = true
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting voice assessment result for LearnerLanguageId {LearnerLanguageId}", request.LearnerLanguageId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi chấp nhận kết quả voice assessment"
                });
            }
        }

        /// <summary>
        /// Reject voice assessment result - Xóa kết quả và cho phép làm lại
        /// </summary>
        [HttpPost("reject-voice-assessment")]
        public async Task<IActionResult> RejectVoiceAssessmentResult([FromBody] AcceptVoiceAssessmentRequestDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Validate the LearnerLanguage exists and belongs to the user
                var learnerLanguage = await _unitOfWork.LearnerLanguages.GetByIdAsync(request.LearnerLanguageId);
                if (learnerLanguage == null || learnerLanguage.UserId != userId)
                {
                    return BadRequest(new { success = false, message = "LearnerLanguage không tồn tại hoặc không thuộc về bạn" });
                }

                // Clear assessment result from Redis
                await _voiceAssessmentService.ClearAssessmentResultAsync(userId, learnerLanguage.LanguageId);

                return Ok(new
                {
                    success = true,
                    message = "Đã từ chối kết quả. Bạn có thể làm lại voice assessment từ đầu.",
                    data = new
                    {
                        learnerLanguageId = request.LearnerLanguageId,
                        languageId = learnerLanguage.LanguageId,
                        canRetakeAssessment = true,
                        mustStartFromBeginning = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting voice assessment for LearnerLanguageId {LearnerLanguageId}", request.LearnerLanguageId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đá xảy ra lỗi khi từ chối kết quả voice assessment"
                });
            }
        }

        /// <summary>
        /// Private method to save accepted assessment result to database
        /// </summary>
        private async Task SaveAcceptedAssessmentToDatabase(
     LearnerLanguage learnerLanguage,
     VoiceAssessmentResultDto assessmentResult)
        {
            try
            {
                _logger.LogInformation("🗺️ Starting SaveAcceptedAssessmentToDatabase for LearnerLanguageId: {LearnerLanguageId}",
                    learnerLanguage.LearnerLanguageId);

              
                learnerLanguage.ProficiencyLevel = assessmentResult.DeterminedLevel;
                learnerLanguage.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.LearnerLanguages.Update(learnerLanguage);

         
                var roadmap = new Roadmap
                {
                    RoadmapID = Guid.NewGuid(),
                    LearnerLanguageId = learnerLanguage.LearnerLanguageId,  // ✅ SỬA TÊN PROPERTY
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("📝 Creating Roadmap: ID={RoadmapId}, LearnerLanguageId={LearnerLanguageId}",
                    roadmap.RoadmapID, roadmap.LearnerLanguageId);

                await _unitOfWork.Roadmaps.CreateAsync(roadmap);

                // 3. Create RoadmapDetails for recommended courses
                var recommendedCourses = assessmentResult.RecommendedCourses ?? new List<RecommendedCourseDto>();

                _logger.LogInformation("📚 Creating {Count} RoadmapDetails", recommendedCourses.Count);

                foreach (var course in recommendedCourses.Where(rc => rc.CourseId != Guid.Empty))
                {
                    var roadmapDetail = new RoadmapDetail
                    {
                        RoadmapDetailID = Guid.NewGuid(),
                        RoadmapID = roadmap.RoadmapID,
                        CourseId = course.CourseId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.RoadmapDetails.CreateAsync(roadmapDetail);

                    _logger.LogInformation("✅ Created RoadmapDetail: CourseId={CourseId}", course.CourseId);
                }

            
                var existingBalanceList = await _unitOfWork.LearnerSlotBalances
                    .FindAsync(lsb => lsb.LearnerId == learnerLanguage.LearnerLanguageId);

                var allBalances = await _unitOfWork.LearnerSlotBalances.GetAllAsync();
                var existingBalance = allBalances?.FirstOrDefault(lsb =>
                    lsb.LearnerId == learnerLanguage.LearnerLanguageId);

                if (existingBalance == null)
                {
                   
                    _logger.LogInformation("💰 Creating NEW LearnerSlotBalance for LearnerId={LearnerId}",
                        learnerLanguage.LearnerLanguageId);

                    var slotBalance = new LearnerSlotBalance
                    {
                        LearnerSlotBalanceId = Guid.NewGuid(),
                        LearnerId = learnerLanguage.LearnerLanguageId,  
                        TotalSlots = 0,
                        UsedSlots = 0,
                        RemainingSlots = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // ✅ LOG AFTER SET để confirm
                    _logger.LogInformation("✅ SlotBalance object created: LearnerSlotBalanceId={Id}, LearnerId={LearnerId}, TotalSlots={Total}",
                        slotBalance.LearnerSlotBalanceId,
                        slotBalance.LearnerId,
                        slotBalance.TotalSlots);

                    // ✅ THÊM VÀO CONTEXT TRƯỚC
                    await _unitOfWork.LearnerSlotBalances.CreateAsync(slotBalance);

                    _logger.LogInformation("✅ SlotBalance added to context, preparing to save...");
                }
                else
                {
                    existingBalance.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.LearnerSlotBalances.Update(existingBalance);

                    _logger.LogInformation("✅ Updated existing LearnerSlotBalance: Id={Id}",
                        existingBalance.LearnerSlotBalanceId);
                }

                // ✅ LOG TRƯỚC KHI SAVE
                _logger.LogInformation("💾 About to call SaveChangesAsync...");

                // 5. Save all changes
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅✅✅ SaveChangesAsync completed successfully!");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "❌ Database update exception in SaveAcceptedAssessmentToDatabase. Inner: {Inner}",
                    dbEx.InnerException?.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in SaveAcceptedAssessmentToDatabase");
                throw;
            }
        }

        /// <summary>
        /// Helper method to determine next target level
        /// </summary>
        private string GetNextTargetLevel(string currentLevel, string languageName)
        {
            if (languageName.Contains("English") || languageName.Contains("Anh"))
            {
                return currentLevel.ToUpper() switch
                {
                    "A1" => "A2",
                    "A2" => "B1",
                    "B1" => "B2",
                    "B2" => "C1",
                    "C1" => "C2",
                    "C2" => "Native-like proficiency",
                    _ => "B1"
                };
            }
            else if (languageName.Contains("Chinese") || languageName.Contains("Trung"))
            {
                return currentLevel.ToUpper() switch
                {
                    "HSK 1" => "HSK 2",
                    "HSK 2" => "HSK 3",
                    "HSK 3" => "HSK 4",
                    "HSK 4" => "HSK 5",
                    "HSK 5" => "HSK 6",
                    "HSK 6" => "Native-like proficiency",
                    _ => "HSK 3"
                };
            }
            else if (languageName.Contains("Japanese") || languageName.Contains("Nhật"))
            {
                return currentLevel.ToUpper() switch
                {
                    "N5" => "N4",
                    "N4" => "N3",
                    "N3" => "N2",
                    "N2" => "N1",
                    "N1" => "Native-like proficiency",
                    _ => "N3"
                };
            }

            return "Intermediate";
        }

        /// <summary>
        /// Test Redis connection
        /// </summary>
        [HttpGet("test-redis")]
        public async Task<IActionResult> TestRedis([FromServices] IDistributedCache cache)
        {
            try
            {
                var testKey = "test_connection";
                var testValue = $"Connected at {DateTime.UtcNow:O}";

                // Write to Redis
                await cache.SetStringAsync(testKey, testValue);

                // Read from Redis
                var retrieved = await cache.GetStringAsync(testKey);

                return Ok(new
                {
                    success = true,
                    message = "✅ Redis connection successful!",
                    data = new
                    {
                        testKey,
                        testValue,
                        retrieved,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "❌ Redis connection failed",
                    error = ex.Message
                });
            }
        }
    }
}