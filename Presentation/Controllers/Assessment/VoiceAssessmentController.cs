    using BLL.IServices.AI;
    using BLL.IServices.Assessment;
    using BLL.IServices.Survey;
    using BLL.IServices.UserGoal;
    using Common.DTO.Assement;
    using Common.DTO.Learner;
    using DAL.Models;
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
            private readonly IUserGoalService _userGoalService;
            public VoiceAssessmentController(
                IVoiceAssessmentService voiceAssessmentService,
                IUserSurveyService userSurveyService,
                IUnitOfWork unitOfWork,
                ILogger<VoiceAssessmentController> logger, IGeminiService geminiService, IUserGoalService userGoalService) 
            {
                _voiceAssessmentService = voiceAssessmentService;
                _userSurveyService = userSurveyService;
                _unitOfWork = unitOfWork;
                _logger = logger;
                _geminiService = geminiService;
                _userGoalService = userGoalService;
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

                    // Check UserGoal đã accepted chưa
                    var existingUserGoal = await _userGoalService.GetUserGoalByLanguageAsync(userId, languageId);

                    if (existingUserGoal != null && !existingUserGoal.IsVoiceAssessmentPending)
                    {
                        // Đã complete và accept rồi
                        return Ok(new
                        {
                            success = true,
                            message = $"Bạn đã hoàn thành đánh giá {language.LanguageName}",
                            action = "completed",
                            data = new
                            {
                                userGoalId = existingUserGoal.UserGoalID,
                                languageId = languageId,
                                languageName = language.LanguageName,
                                determinedLevel = existingUserGoal.DeterminedLevel,
                                roadmap = existingUserGoal.RoadmapData,
                                completedAt = existingUserGoal.CreatedAt
                            }
                        });
                    }

                    // Nếu có pending assessment, xóa đi và bắt đầu mới
                    if (existingUserGoal != null && existingUserGoal.IsVoiceAssessmentPending)
                    {
                        await _userGoalService.RejectVoiceAssessmentResultAsync(existingUserGoal.UserGoalID, userId);
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
            /// Skip voice assessment - Bỏ qua đánh giá giọng nói
            /// </summary>
            [HttpPost("skip/{languageId:guid}")]
            public async Task<IActionResult> SkipVoiceAssessment(Guid languageId, [FromQuery] int? goalId = null)
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
                            message = "Ngôn ngữ không tồn tại"
                        });
                    }

                    // Tạo UserGoal với level mặc định (skipped)
                    var skippedUserGoal = await _userGoalService.CreateSkippedVoiceAssessmentAsync(
                        userId,
                        languageId,
                        language.LanguageName,
                        goalId
                    );

                    return Ok(new
                    {
                        success = true,
                        message = "Đã bỏ qua đánh giá giọng nói. Bạn có thể bắt đầu học ngay.",
                        action = "skipped",
                        data = new
                        {
                            userGoalId = skippedUserGoal.UserGoalID,
                            languageId = languageId,
                            languageName = language.LanguageName,
                            determinedLevel = "Not Assessed",
                            roadmap = skippedUserGoal.RoadmapData,
                            courseRecommendations = skippedUserGoal.RecommendedCoursesData
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error skipping voice assessment");
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
            /// Accept voice assessment result và lưu vào DB
            /// </summary>
            [HttpPost("accept-voice-assessment")]
            public async Task<IActionResult> AcceptVoiceAssessmentResult([FromBody] AcceptVoiceAssessmentRequestDto request)
            {
                try
                {
                    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                    await _userGoalService.AcceptVoiceAssessmentResultAsync(request.UserGoalId, userId);

                    return Ok(new
                    {
                        success = true,
                        message = "Đã chấp nhận và lưu kết quả voice assessment thành công! Roadmap của bạn đã được tạo.",
                        data = new { userGoalId = request.UserGoalId }
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

                
                    await _userGoalService.RejectVoiceAssessmentResultAsync(request.UserGoalId, userId);

                    return Ok(new
                    {
                        success = true,
                        message = "Đã từ chối kết quả. Bạn cần làm lại voice assessment từ đầu.",
                        data = new
                        {
                            userGoalId = request.UserGoalId,
                            canRetakeAssessment = true,
                            mustStartFromBeginning = true 
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rejecting voice assessment");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Đã xảy ra lỗi khi từ chối kết quả voice assessment"
                    });
                }
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

