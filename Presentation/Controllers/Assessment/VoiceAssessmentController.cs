using BLL.IServices.AI;
using BLL.IServices.Assessment;
using BLL.IServices.Survey;
using Common.DTO.Assement;
using Common.DTO.Learner;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace Presentation.Controllers.Assessment
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VoiceAssessmentController : ControllerBase
    {
        private readonly IVoiceAssessmentService _voiceAssessmentService;
        private readonly IUserSurveyService _userSurveyService; 
        private readonly IUnitOfWork _unitOfWork; 
        private readonly ILogger<VoiceAssessmentController> _logger;
        private readonly IGeminiService _geminiService;

        public VoiceAssessmentController(
            IVoiceAssessmentService voiceAssessmentService,
            IUserSurveyService userSurveyService,
            IUnitOfWork unitOfWork,
            ILogger<VoiceAssessmentController> logger, IGeminiService geminiService) 
        {
            _voiceAssessmentService = voiceAssessmentService;
            _userSurveyService = userSurveyService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _geminiService = geminiService;
        }

        /// <summary>
        /// Bắt đầu bài đánh giá giọng nói - TỰ ĐỘNG LƯU VÀO REDIS
        /// </summary>
        [HttpPost("start/{languageId:guid}")]
        public async Task<IActionResult> StartVoiceAssessment(Guid languageId, [FromQuery] int? goalId = null)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var assessment = await _voiceAssessmentService.StartVoiceAssessmentAsync(userId, languageId, goalId);

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
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy câu hỏi hiện tại
        /// </summary>
        /// <summary>
        /// 🎯 Lấy câu hỏi hiện tại CÓ VIETNAMESE SUPPORT
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
                    return Forbid("Bạn không có quyền truy cập assessment này");
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
        /// Gửi file âm thanh trả lời (xử lý trực tiếp bằng Gemini AI)
        /// </summary>
        /// <param name="assessmentId">ID của bài đánh giá (GUID)</param>
        /// <param name="formDto">Dữ liệu form với file âm thanh</param>
        /// <summary>
        /// 📤 Submit voice với validation mạnh
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

                var isValid = await _voiceAssessmentService.ValidateAssessmentIdAsync(assessmentId, userId);
                if (!isValid)
                {
                    return Forbid("Bạn không có quyền truy cập assessment này");
                }

                if (!formDto.IsSkipped && formDto.AudioFile == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn phải gửi file âm thanh hoặc chọn bỏ qua câu hỏi",
                        errorCode = "AUDIO_FILE_REQUIRED"
                    });
                }

                if (formDto.AudioFile != null)
                {
                    var allowedTypes = new[] { "audio/mp3", "audio/wav", "audio/m4a", "audio/webm", "audio/mpeg" };
                    if (!allowedTypes.Contains(formDto.AudioFile.ContentType.ToLower()))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Chỉ hỗ trợ file âm thanh: MP3, WAV, M4A, WebM",
                            errorCode = "INVALID_AUDIO_FORMAT"
                        });
                    }

                    if (formDto.AudioFile.Length > 10 * 1024 * 1024)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "File âm thanh không được vượt quá 10MB",
                            errorCode = "FILE_TOO_LARGE"
                        });
                    }

                    // ✅ SỬA: Lấy LanguageCode từ assessment thông qua VoiceAssessmentService
                    var question = await _voiceAssessmentService.GetCurrentQuestionAsync(assessmentId);
                    var assessment = await _voiceAssessmentService.RestoreAssessmentFromIdAsync(assessmentId);

                    // ✅ Lấy Language từ database qua LanguageId của assessment
                    if (assessment == null)
                    {
                        return BadRequest(new { success = false, message = "Assessment không tồn tại" });
                    }

                    // Giả sử VoiceAssessmentDto có LanguageId, nếu không thì cần thêm property này
                    var language = await _unitOfWork.Languages.GetByIdAsync(assessment.LanguageId);
                    if (language == null)
                    {
                        return BadRequest(new { success = false, message = "Ngôn ngữ không tồn tại" });
                    }

                    var languageCode = language.LanguageCode; // EN, ZH, JP

                    var aiResult = await _geminiService.EvaluateVoiceResponseDirectlyAsync(question, formDto.AudioFile, languageCode);

                    // ✅ KIỂM TRA NGÔN NGỮ AUDIO
                    if (aiResult.OverallScore == 0 &&
                        aiResult.Pronunciation != null &&
                        aiResult.Pronunciation.Level == "Language Error")
                    {
                        return BadRequest(new
                        {
                            success = false,
                            error = "LANGUAGE_MISMATCH",
                            message = aiResult.DetailedFeedback
                        });
                    }
                }

                var response = new VoiceAssessmentResponseDto
                {
                    AssessmentId = assessmentId,
                    QuestionNumber = formDto.QuestionNumber,
                    IsSkipped = formDto.IsSkipped,
                    AudioFile = formDto.AudioFile,
                    RecordingDurationSeconds = formDto.RecordingDurationSeconds
                };

                await _voiceAssessmentService.SubmitVoiceResponseAsync(assessmentId, response);

                return Ok(new
                {
                    success = true,
                    message = formDto.IsSkipped ? "Đã bỏ qua câu hỏi" : "Đã đánh giá giọng nói thành công",
                    data = new
                    {
                        assessmentId = assessmentId,
                        questionNumber = formDto.QuestionNumber,
                        processed = !formDto.IsSkipped,
                        fileSize = formDto.AudioFile?.Length ?? 0,
                        aiProcessed = !formDto.IsSkipped && formDto.AudioFile != null
                    }
                });
            }
            catch (Exception ex)
            {
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

                var supportedLanguages = new[] { "EN", "ZH", "JP" };
                if (!supportedLanguages.Contains(language.LanguageCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chỉ hỗ trợ đánh giá giọng nói tiếng Anh, tiếng Trung và tiếng Nhật",
                        errorCode = "LANGUAGE_NOT_SUPPORTED"
                    });
                }

             
                var userSurvey = await _userSurveyService.GetUserSurveyAsync(userId);
                bool isLanguageChange = false;

                if (userSurvey != null && userSurvey.PreferredLanguageID != languageId)
                {
                    isLanguageChange = true;

                 
                    _logger.LogInformation("🔄 User {UserId} đổi ngôn ngữ từ {OldLang} sang {NewLang}",
                        userId, userSurvey.PreferredLanguageID, languageId);

                    await _userSurveyService.UpdatePreferredLanguageAsync(userId, languageId);

                  
                    await _voiceAssessmentService.ClearAssessmentResultAsync(userId, userSurvey.PreferredLanguageID);
                }

        
                var existingAssessmentId = await _voiceAssessmentService.FindAssessmentIdAsync(userId, languageId);
                var hasCompletedResult = await _voiceAssessmentService.HasCompletedVoiceAssessmentAsync(userId, languageId);

           
                if (hasCompletedResult)
                {
               
                    var existingResult = await _voiceAssessmentService.GetVoiceAssessmentResultAsync(userId, languageId);

                    return Ok(new
                    {
                        success = true,
                        message = $"Bạn đã hoàn thành đánh giá {language.LanguageName}",
                        action = "completed",
                        isLanguageChange = isLanguageChange,
                        data = new
                        {
                            languageId = languageId,
                            languageName = language.LanguageName,
                            result = existingResult,
                            completedAt = existingResult?.CompletedAt
                        }
                    });
                }

                if (existingAssessmentId.HasValue)
                {
           
                    var existingAssessment = await _voiceAssessmentService.RestoreAssessmentFromIdAsync(existingAssessmentId.Value);
                    if (existingAssessment != null)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = $"Tiếp tục đánh giá {language.LanguageName}",
                            action = "resumed",
                            isLanguageChange = isLanguageChange,
                            data = existingAssessment
                        });
                    }
                }

            
                var newAssessment = await _voiceAssessmentService.StartVoiceAssessmentAsync(userId, languageId, goalId);

                return Ok(new
                {
                    success = true,
                    message = isLanguageChange
                        ? $"🔄 Bắt đầu đánh giá {language.LanguageName} sau khi đổi ngôn ngữ"
                        : $"🎯 Bắt đầu đánh giá {language.LanguageName}",
                    action = "started",
                    isLanguageChange = isLanguageChange,
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
        /// Hoàn thành bài đánh giá giọng nói và nhận kết quả
        /// </summary>
        /// <summary>
        /// Hoàn thành bài đánh giá giọng nói và nhận kết quả + đề xuất khóa học
        /// </summary>
        [HttpPost("{assessmentId:guid}/complete")]
        public async Task<IActionResult> CompleteVoiceAssessment(Guid assessmentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

             
                var result = await _voiceAssessmentService.CompleteVoiceAssessmentAsync(assessmentId);

         
                AiCourseRecommendationDto? courseRecommendations = null;
                try
                {
                    var userSurvey = await _userSurveyService.GetUserSurveyAsync(userId);
                    if (userSurvey != null)
                    {
                        courseRecommendations = await _userSurveyService.GenerateRecommendationsAsync(userId);

                        _logger.LogInformation("✅ Generated {Count} course recommendations for user {UserId}",
                            courseRecommendations.RecommendedCourses?.Count ?? 0, userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not generate course recommendations for user {UserId}", userId);
                }

                return Ok(new
                {
                    success = true,
                    message = "Hoàn thành đánh giá giọng nói thành công!",
                    data = new
                    {
                       
                        voiceResult = result,

                 
                        courseRecommendations = courseRecommendations,

                    
                        summary = new
                        {
                            languageName = result.LanguageName,
                            determinedLevel = result.DeterminedLevel,
                            overallScore = result.OverallScore,
                            hasRecommendations = courseRecommendations?.RecommendedCourses?.Any() == true,
                            recommendedCoursesCount = courseRecommendations?.RecommendedCourses?.Count ?? 0
                        }
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
        /// Debug endpoint - kiểm tra trạng thái active assessments (for mobile development)
        /// </summary>
       
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

