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
using BLL.IServices.Redis;
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
        private readonly IRedisService _redisService;
        public VoiceAssessmentController(
            IVoiceAssessmentService voiceAssessmentService,
            IUnitOfWork unitOfWork,
            ICourseRecommendationService courseRecommendationService,
            ILogger<VoiceAssessmentController> logger, IRedisService redisService)
        {
            _logger = logger;
            _voiceAssessmentService = voiceAssessmentService;
            _unitOfWork = unitOfWork;
            _courseRecommendationService = courseRecommendationService;
            _redisService = redisService;
        }

        /// <summary>
        /// Bắt đầu bài đánh giá giọng nói - TỰ ĐỘNG LƯU VÀO REDIS
        /// </summary>
      //  [HttpPost("start")]
      //  public async Task<IActionResult> StartVoiceAssessment(
      //[FromQuery] Guid languageId,  // ✅ Query parameter
      //[FromQuery] int? goalId = null)  // ✅ Optional
      //  {
      //      try
      //      {
      //          var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

      //          _logger.LogInformation("Starting assessment: UserId={UserId}, LanguageId={LanguageId}, GoalId={GoalId}",
      //              userId, languageId, goalId);

      //          var assessment = await _voiceAssessmentService.StartVoiceAssessmentAsync(
      //              userId,
      //              languageId,
      //              goalId);

      //          return Ok(new
      //          {
      //              success = true,
      //              message = "Bắt đầu voice assessment thành công",
      //              data = assessment
      //          });
      //      }
      //      catch (InvalidOperationException ex) when (ex.Message.Contains("đã hoàn thành và chấp nhận"))
      //      {
      //          return BadRequest(new
      //          {
      //              success = false,
      //              message = ex.Message,
      //              errorCode = "ASSESSMENT_ALREADY_ACCEPTED",
      //              canRetake = false
      //          });
      //      }
      //      catch (ArgumentException ex)
      //      {
      //          return BadRequest(new
      //          {
      //              success = false,
      //              message = ex.Message
      //          });
      //      }
      //      catch (Exception ex)
      //      {
      //          _logger.LogError(ex, "Error starting voice assessment");
      //          return StatusCode(500, new
      //          {
      //              success = false,
      //              message = "Đã xảy ra lỗi khi bắt đầu assessment",
      //              error = ex.Message
      //          });
      //      }
      //  }

        [HttpGet("check-required")]
        public async Task<IActionResult> CheckAssessmentRequired()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("Checking if assessment is required for user {UserId}", userId);

                // Lấy tất cả LearnerLanguages của user
                var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                var userLearnerLanguages = allLearnerLanguages
                    .Where(ll => ll.UserId == userId)
                    .ToList();

                // Nếu user chưa có LearnerLanguage nào → PHẢI LÀM ASSESSMENT
                if (!userLearnerLanguages.Any())
                {
                    _logger.LogInformation("User {UserId} has NO LearnerLanguages. Assessment REQUIRED.", userId);

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            assessmentRequired = true,
                            hasAcceptedAssessment = false,
                            reason = "Bạn chưa chọn ngôn ngữ học. Vui lòng làm assessment để bắt đầu.",
                            nextAction = "SELECT_LANGUAGE_AND_GOALS"  // Updated message
                        }
                    });
                }

                // Kiểm tra xem có LearnerLanguage nào đã có Roadmap chưa
                var allRoadmaps = await _unitOfWork.Roadmaps.GetAllAsync();
                var hasAcceptedRoadmap = userLearnerLanguages.Any(ll =>
                    allRoadmaps.Any(r => r.LearnerLanguageId == ll.LearnerLanguageId));

                // Get all learner goals for this user (declare once at method level)
                var allLearnerGoals = await _unitOfWork.LearnerGoals.GetAllAsync();

                if (hasAcceptedRoadmap)
                {
                    // User đã accept ít nhất 1 assessment → KHÔNG CẦN LÀM NỮA
                    _logger.LogInformation("User {UserId} has ACCEPTED assessment(s). No action required.", userId);

                    var acceptedLanguages = new List<object>();
                    foreach (var ll in userLearnerLanguages.Where(ll => allRoadmaps.Any(r => r.LearnerLanguageId == ll.LearnerLanguageId)))
                    {
                        var language = await _unitOfWork.Languages.GetByIdAsync(ll.LanguageId);

                        // Get all goals for this learner language
                        var learnerGoals = allLearnerGoals.Where(lg => lg.LearnerId == ll.LearnerLanguageId).ToList();

                        acceptedLanguages.Add(new
                        {
                            learnerLanguageId = ll.LearnerLanguageId,
                            languageId = ll.LanguageId,
                            languageName = language?.LanguageName,
                            proficiencyLevel = ll.ProficiencyLevel,
                            goalIds = learnerGoals.Select(lg => lg.GoalId).ToList(),
                            goalCount = learnerGoals.Count
                        });
                    }

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            assessmentRequired = false,
                            hasAcceptedAssessment = true,
                            acceptedLanguages = acceptedLanguages,
                            reason = "Bạn đã hoàn thành assessment và có roadmap học tập.",
                            nextAction = "GO_TO_HOME"
                        }
                    });
                }

                // User có LearnerLanguage nhưng CHƯA accept → CÓ THỂ LÀM LẠI
                _logger.LogInformation("User {UserId} has LearnerLanguages but NO accepted roadmap. Can retake assessment.", userId);

                var incompleteLearnerLanguages = new List<object>();

                foreach (var ll in userLearnerLanguages)
                {
                    // Get all goals for this learner language
                    var learnerGoals = allLearnerGoals.Where(lg => lg.LearnerId == ll.LearnerLanguageId).ToList();

                    incompleteLearnerLanguages.Add(new
                    {
                        learnerLanguageId = ll.LearnerLanguageId,
                        languageId = ll.LanguageId,
                        proficiencyLevel = ll.ProficiencyLevel,
                        goalIds = learnerGoals.Select(lg => lg.GoalId).ToList(),
                        goalCount = learnerGoals.Count
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        assessmentRequired = true,
                        hasAcceptedAssessment = false,
                        hasIncompleteLearnerLanguages = true,
                        incompleteLearnerLanguages = incompleteLearnerLanguages,
                        reason = "Bạn đã bắt đầu nhưng chưa hoàn tất việc chấp nhận kết quả assessment.",
                        nextAction = "REVIEW_OR_RETAKE"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking assessment requirement");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi",
                    error = ex.Message
                });
            }
        }
        // <summary>
        /// Bắt đầu bài đánh giá giọng nói với nhiều goals - TỰ ĐỘNG LƯU VÀO REDIS
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartVoiceAssessment(
            [FromQuery] Guid languageId,  // ✅ Query parameter
            [FromQuery] List<int>? goalIds = null)  // ✅ Support multiple goals
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("Starting assessment: UserId={UserId}, LanguageId={LanguageId}, GoalIds={GoalIds}",
                    userId, languageId, goalIds != null ? string.Join(",", goalIds) : "null");

                var assessment = await _voiceAssessmentService.StartVoiceAssessmentAsync(
                    userId,
                    languageId,
                    goalIds);

                return Ok(new
                {
                    success = true,
                    message = "Bắt đầu voice assessment thành công",
                    data = assessment
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("đã hoàn thành và chấp nhận"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    errorCode = "ASSESSMENT_ALREADY_ACCEPTED",
                    canRetake = false
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting voice assessment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi bắt đầu assessment",
                    error = ex.Message
                });
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

        [HttpPost("complete/{assessmentId:guid}")]
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

                var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                var learnerLanguage = allLearnerLanguages.FirstOrDefault(ll =>
                    ll.UserId == userId &&
                    ll.LanguageId == assessment.LanguageId);

                if (learnerLanguage == null)
                {
                    learnerLanguage = new LearnerLanguage
                    {
                        LearnerLanguageId = Guid.NewGuid(),
                        UserId = userId,
                        LanguageId = assessment.LanguageId,
                        ProficiencyLevel = "Pending Assessment",
                        StreakDays = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.LearnerLanguages.CreateAsync(learnerLanguage);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("✅ Created NEW LearnerLanguage: Id={Id}, Language={Lang}",
                        learnerLanguage.LearnerLanguageId,
                        assessment.LanguageName);
                }

                // Handle multiple goals relationship through LearnerGoal
                if (assessment.GoalIds.Any())
                {
                    var allLearnerGoals = await _unitOfWork.LearnerGoals.GetAllAsync();

                    // Remove existing goals for this learner language
                    var existingLearnerGoals = allLearnerGoals.Where(lg =>
                        lg.LearnerId == learnerLanguage.LearnerLanguageId).ToList();

                    foreach (var existingGoal in existingLearnerGoals)
                    {
                        _unitOfWork.LearnerGoals.Remove(existingGoal);
                        _logger.LogInformation(" Deleted existing LearnerGoal: Id={Id}, Goal={Goal}",
                            existingGoal.LearnerGoalId, existingGoal.GoalId);
                    }

                    // Create new LearnerGoal entries for each selected goal
                    foreach (var goalId in assessment.GoalIds)
                    {
                        var learnerGoal = new LearnerGoal
                        {
                            LearnerGoalId = Guid.NewGuid(),
                            LearnerId = learnerLanguage.LearnerLanguageId,
                            GoalId = goalId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.LearnerGoals.CreateAsync(learnerGoal);
                        _logger.LogInformation("✅ Created NEW LearnerGoal: Id={Id}, Goal={Goal}",
                            learnerGoal.LearnerGoalId, goalId);
                    }

                    await _unitOfWork.SaveChangesAsync();
                }

                // Get the first goal ID for course recommendations (backward compatibility)
                var currentGoalId = assessment.GoalIds.FirstOrDefault() == 0 ? (int?)null : assessment.GoalIds.FirstOrDefault();
                var recommendedCourses = await _courseRecommendationService.GetRecommendedCoursesAsync(
                    assessment.LanguageId,
                    assessmentResult.OverallLevel,
                    currentGoalId);

                _logger.LogInformation("Found {Count} recommended courses for level {Level}",
                    recommendedCourses.Count, assessmentResult.OverallLevel);

                var hasCoursesForLevel = await _courseRecommendationService.HasCoursesForLevelAsync(
                    assessment.LanguageId,
                    assessmentResult.OverallLevel);

                await _voiceAssessmentService.SaveRecommendedCoursesAsync(
                    userId,
                    assessment.LanguageId,
                    recommendedCourses);

                var goalNamesText = assessment.GoalNames.Any() ? string.Join(", ", assessment.GoalNames) : "không xác định";
                var message = hasCoursesForLevel && recommendedCourses.Any()
                    ? $"Hoàn thành voice assessment thành công! Tìm thấy {recommendedCourses.Count} khóa học phù hợp với trình độ {assessmentResult.OverallLevel} và mục tiêu {goalNamesText} của bạn."
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
                        goalIds = assessment.GoalIds,
                        goalNames = assessment.GoalNames,
                        goalId = currentGoalId, // Backward compatibility
                        goalName = assessment.GoalName, // Backward compatibility
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

               
                var recommendedCoursesFromRedis = await GetRecommendedCoursesFromRedis(userId, learnerLanguage.LanguageId);

              
                assessmentResult.RecommendedCourses = recommendedCoursesFromRedis.Select(rc => new RecommendedCourseDto
                {
                    CourseId = rc.CourseID,  
                    CourseName = rc.CourseName,
                    Level = rc.Level,
                    MatchReason = rc.MatchReason
                }).ToList();

                await SaveAcceptedAssessmentToDatabase(learnerLanguage, assessmentResult);
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user != null)
                {
                    user.ActiveLanguageId = learnerLanguage.LanguageId;
                    user.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("✅ Updated ActiveLanguageId for user {UserId} to {LanguageId}",
                        userId, learnerLanguage.LanguageId);
                }

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
                        slotBalanceCreated = true,
                        activeLanguageId = learnerLanguage.LanguageId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting voice assessment result");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi chấp nhận kết quả voice assessment"
                });
            }
        }

        private async Task<List<CourseRecommendationDto>> GetRecommendedCoursesFromRedis(Guid userId, Guid languageId)
        {
            try
            {
                var cacheKey = $"voice_assessment_recommended_courses:{userId}:{languageId}";
                var courses = await _redisService.GetAsync<List<CourseRecommendationDto>>(cacheKey);

                _logger.LogInformation("📚 Retrieved {Count} courses from Redis key: {Key}",
                    courses?.Count ?? 0, cacheKey);

                return courses ?? new List<CourseRecommendationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended courses from Redis");
                return new List<CourseRecommendationDto>();
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
                    message = "Đã xảy ra lỗi khi từ chối kết quả voice assessment"
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

                // ✅ 1. Update LearnerLanguage ProficiencyLevel
                learnerLanguage.ProficiencyLevel = assessmentResult.DeterminedLevel;
                learnerLanguage.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.LearnerLanguages.Update(learnerLanguage);

                // ✅ 2. Create Roadmap
                var roadmap = new Roadmap
                {
                    RoadmapID = Guid.NewGuid(),
                    LearnerLanguageId = learnerLanguage.LearnerLanguageId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("📝 Creating Roadmap: ID={RoadmapId}, LearnerLanguageId={LearnerLanguageId}",
                    roadmap.RoadmapID, roadmap.LearnerLanguageId);

                await _unitOfWork.Roadmaps.CreateAsync(roadmap);

                // ✅ 3. SAVE ROADMAP FIRST before creating RoadmapDetails
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("✅ Roadmap saved successfully");

                // ✅ 4. Get RecommendedCourses with detailed logging
                var recommendedCourses = assessmentResult.RecommendedCourses ?? new List<RecommendedCourseDto>();

                _logger.LogInformation("📚 RecommendedCourses Count: {Count}", recommendedCourses.Count);

                if (recommendedCourses.Any())
                {
                    foreach (var course in recommendedCourses)
                    {
                        _logger.LogInformation("Course Info: CourseId={CourseId}, CourseName={CourseName}",
                            course.CourseId, course.CourseName ?? "NULL");
                    }
                }

                // ✅ 5. Filter valid courses and create RoadmapDetails
                var validCourses = recommendedCourses.Where(rc => rc.CourseId != Guid.Empty).ToList();

                _logger.LogInformation("📚 Valid courses to create RoadmapDetails: {Count}", validCourses.Count);

                if (validCourses.Any())
                {
                    foreach (var course in validCourses)
                    {
                        // ✅ Verify Course exists in database
                        var courseExists = await _unitOfWork.Courses.GetByIdAsync(course.CourseId);
                        if (courseExists == null)
                        {
                            _logger.LogWarning("⚠️ Course {CourseId} not found in database, skipping", course.CourseId);
                            continue;
                        }

                        var roadmapDetail = new RoadmapDetail
                        {
                            RoadmapDetailID = Guid.NewGuid(),
                            RoadmapID = roadmap.RoadmapID,
                            CourseId = course.CourseId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.RoadmapDetails.CreateAsync(roadmapDetail);

                        _logger.LogInformation("✅ Created RoadmapDetail: ID={RoadmapDetailId}, CourseId={CourseId}, CourseName={CourseName}",
                            roadmapDetail.RoadmapDetailID, course.CourseId, course.CourseName);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("✅ All RoadmapDetails saved successfully");
                }
                else
                {
                    _logger.LogWarning("⚠️ No valid courses to create RoadmapDetails");
                }

                _logger.LogInformation("✅✅✅ SaveAcceptedAssessmentToDatabase completed successfully!");
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
                var retrieved = await cache.GetStringAsync(testValue);

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

        [HttpGet("my-assessment-status")]
        public async Task<IActionResult> GetMyAssessmentStatus()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var allLanguages = await _unitOfWork.Languages.GetAllAsync();
                var assessmentStatuses = new List<object>();

                foreach (var language in allLanguages)
                {
                    var hasCompleted = await _voiceAssessmentService.HasCompletedVoiceAssessmentAsync(userId, language.LanguageID);

                    VoiceAssessmentResultDto? result = null;
                    if (hasCompleted)
                    {
                        result = await _voiceAssessmentService.GetVoiceAssessmentResultAsync(userId, language.LanguageID);
                    }

                    var activeAssessments = await _redisService.GetUserAssessmentsAsync(userId, language.LanguageID);
                    var activeAssessment = activeAssessments.FirstOrDefault();

                    // ✅ CHECK XEM ĐÃ ACCEPT CHƯA (có Roadmap)
                    var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                    var learnerLanguage = allLearnerLanguages.FirstOrDefault(ll =>
                        ll.UserId == userId && ll.LanguageId == language.LanguageID);

                    bool hasAccepted = false;
                    Guid? roadmapId = null;

                    if (learnerLanguage != null)
                    {
                        var allRoadmaps = await _unitOfWork.Roadmaps.GetAllAsync();
                        var roadmap = allRoadmaps.FirstOrDefault(r =>
                            r.LearnerLanguageId == learnerLanguage.LearnerLanguageId);

                        if (roadmap != null)
                        {
                            hasAccepted = true;
                            roadmapId = roadmap.RoadmapID;
                        }
                    }

                    // Get goal information
                    int? goalId = null;
                    if (learnerLanguage != null)
                    {
                        var allLearnerGoals = await _unitOfWork.LearnerGoals.GetAllAsync();
                        var learnerGoal = allLearnerGoals.FirstOrDefault(lg => lg.LearnerId == learnerLanguage.LearnerLanguageId);
                        goalId = learnerGoal?.GoalId;
                    }

                    assessmentStatuses.Add(new
                    {
                        languageId = language.LanguageID,
                        languageName = language.LanguageName,
                        languageCode = language.LanguageCode,

                        // Status
                        hasCompletedAssessment = hasCompleted,
                        hasActiveAssessment = activeAssessment != null,
                        hasAcceptedResult = hasAccepted,  // ✅ FLAG MỚI

                        // IDs
                        activeAssessmentId = activeAssessment?.AssessmentId,
                        learnerLanguageId = learnerLanguage?.LearnerLanguageId,
                        roadmapId = roadmapId,  // ✅ ROADMAP ID
                        goalId = goalId, // ✅ GOAL ID from LearnerGoal

                        // Assessment info
                        currentQuestionIndex = activeAssessment?.CurrentQuestionIndex,
                        totalQuestions = activeAssessment?.Questions.Count,

                        // Result info
                        determinedLevel = result?.DeterminedLevel ?? learnerLanguage?.ProficiencyLevel,
                        overallScore = result?.OverallScore,
                        completedAt = result?.CompletedAt,

                        // Action flags
                        needsSelection = !hasCompleted && activeAssessment == null && !hasAccepted,
                        canResume = activeAssessment != null && !hasAccepted,
                        canReview = hasCompleted,
                        canRetake = !hasAccepted,  // ✅ CHỈ CHO LÀM LẠI NẾU CHƯA ACCEPT
                        isLocked = hasAccepted  // ✅ ĐÃ LOCK
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy trạng thái assessment thành công",
                    data = new
                    {
                        userId = userId,
                        languages = assessmentStatuses
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi",
                    error = ex.Message
                });
            }
        }

        [HttpGet("check-status/{languageId:guid}")]
        public async Task<IActionResult> CheckAssessmentStatus(Guid languageId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation("Checking assessment status for user {UserId}, language {LanguageId}",
                    userId, languageId);

                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Ngôn ngữ không tồn tại"
                    });
                }

                // Check completed
                var hasCompleted = await _voiceAssessmentService.HasCompletedVoiceAssessmentAsync(userId, languageId);

                // Get result if completed
                VoiceAssessmentResultDto? result = null;
                if (hasCompleted)
                {
                    result = await _voiceAssessmentService.GetVoiceAssessmentResultAsync(userId, languageId);
                }

                // Check active assessment
                var activeAssessments = await _redisService.GetUserAssessmentsAsync(userId, languageId);
                var activeAssessment = activeAssessments.FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        languageId = languageId,
                        languageName = language.LanguageName,

                        // Status flags
                        hasCompletedAssessment = hasCompleted,
                        hasActiveAssessment = activeAssessment != null,

                        // Active assessment
                        activeAssessment = activeAssessment != null ? new
                        {
                            assessmentId = activeAssessment.AssessmentId,
                            currentQuestionIndex = activeAssessment.CurrentQuestionIndex,
                            totalQuestions = activeAssessment.Questions.Count,
                            goalId = activeAssessment.GoalID,
                            goalName = activeAssessment.GoalName,
                            createdAt = activeAssessment.CreatedAt
                        } : null,

                        // Completed result
                        completedResult = hasCompleted ? new
                        {
                            determinedLevel = result?.DeterminedLevel,
                            overallScore = result?.OverallScore,
                            completedAt = result?.CompletedAt
                        } : null,

                        // Recommendation
                        recommendation = GetRecommendation(hasCompleted, activeAssessment != null)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking assessment status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi",
                    error = ex.Message
                });
            }
        }

        private string GetRecommendation(bool hasCompleted, bool hasActive)
        {
            if (hasCompleted)
                return "Bạn đã hoàn thành assessment. Có thể xem lại kết quả hoặc làm lại.";

            if (hasActive)
                return "Bạn có bài assessment đang làm dở. Tiếp tục hoặc bắt đầu lại?";

            return "Bạn chưa làm assessment. Hãy chọn goal để bắt đầu!";
        }
        /// <summary>
        /// Check xem user đã hoàn thành & chấp nhận assessment cho ngôn ngữ này chưa
        /// </summary>
        [HttpGet("check-lang-assessment/{languageId:guid}")]
        [Authorize]
        public async Task<IActionResult> CheckLanguageAssessmentStatus(Guid languageId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Ngôn ngữ không tồn tại"
                    });
                }

                // ✅ Kiểm tra xem LearnerLanguage + Roadmap có tồn tại (= đã accept)
                var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                var learnerLanguage = allLearnerLanguages.FirstOrDefault(ll =>
                    ll.UserId == userId && ll.LanguageId == languageId);

                if (learnerLanguage == null)
                {
                    // Chưa có LearnerLanguage = chưa làm assessment hoặc chưa hoàn thành
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            languageId = languageId,
                            languageName = language.LanguageName,
                            hasCompletedAssessment = false,
                            requiresAssessment = true,
                            message = $"Bạn cần hoàn thành bài đánh giá giọng nói cho {language.LanguageName} trước khi tiếp tục học."
                        }
                    });
                }


                var allRoadmaps = await _unitOfWork.Roadmaps.GetAllAsync();
                var roadmap = allRoadmaps.FirstOrDefault(r =>
                    r.LearnerLanguageId == learnerLanguage.LearnerLanguageId);

                if (roadmap != null)
                {

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            languageId = languageId,
                            languageName = language.LanguageName,
                            learnerLanguageId = learnerLanguage.LearnerLanguageId,
                            hasCompletedAssessment = true,
                            requiresAssessment = false,
                            proficiencyLevel = learnerLanguage.ProficiencyLevel,
                            roadmapId = roadmap.RoadmapID,
                            message = $"Bạn đã hoàn thành assessment cho {language.LanguageName}. Mức độ: {learnerLanguage.ProficiencyLevel}"
                        }
                    });
                }


                var activeAssessments = await _redisService.GetUserAssessmentsAsync(userId, languageId);
                var activeAssessment = activeAssessments.FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        languageId = languageId,
                        languageName = language.LanguageName,
                        learnerLanguageId = learnerLanguage.LearnerLanguageId,
                        hasCompletedAssessment = false,
                        requiresAssessment = true,
                        hasActiveAssessment = activeAssessment != null,
                        activeAssessmentId = activeAssessment?.AssessmentId,
                        message = $"Bạn chưa hoàn thành assessment cho {language.LanguageName}. Vui lòng tiếp tục hoặc bắt đầu lại."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking language assessment status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi",
                    error = ex.Message
                });
            }
        }
        /// <summary>
        /// Switch ngôn ngữ - check xem đã làm assessment chưa
        /// </summary>
        [HttpPost("switch-language/{languageId:guid}")]
        [Authorize]
        public async Task<IActionResult> SwitchLanguage(Guid languageId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Ngôn ngữ không tồn tại"
                    });
                }


                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return Unauthorized();

                user.ActiveLanguageId = languageId;
                user.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();


                var allLearnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                var learnerLanguage = allLearnerLanguages.FirstOrDefault(ll =>
                    ll.UserId == userId && ll.LanguageId == languageId);

                if (learnerLanguage == null)
                {
                    return Ok(new
                    {
                        success = true,
                        action = "REQUIRE_ASSESSMENT",
                        message = $"Bạn cần hoàn thành bài đánh giá giọng nói cho {language.LanguageName}",
                        data = new
                        {
                            languageId = languageId,
                            languageName = language.LanguageName,
                            requiresAssessment = true
                        }
                    });
                }

                // Check xem có Roadmap chưa
                var allRoadmaps = await _unitOfWork.Roadmaps.GetAllAsync();
                var roadmap = allRoadmaps.FirstOrDefault(r =>
                    r.LearnerLanguageId == learnerLanguage.LearnerLanguageId);

                if (roadmap != null)
                {
                    // Đã accept rồi
                    return Ok(new
                    {
                        success = true,
                        action = "PROCEED_TO_HOME",
                        message = $"Chào mừng bạn! Bạn đã sẵn sàng học {language.LanguageName}",
                        data = new
                        {
                            languageId = languageId,
                            languageName = language.LanguageName,
                            learnerLanguageId = learnerLanguage.LearnerLanguageId,
                            proficiencyLevel = learnerLanguage.ProficiencyLevel,
                            roadmapId = roadmap.RoadmapID
                        }
                    });
                }

                // Đang làm dở assessment
                return Ok(new
                {
                    success = true,
                    action = "RESUME_ASSESSMENT",
                    message = $"Bạn có bài assessment đang làm dở cho {language.LanguageName}",
                    data = new
                    {
                        languageId = languageId,
                        languageName = language.LanguageName,
                        learnerLanguageId = learnerLanguage.LearnerLanguageId,
                        requiresAssessment = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching language");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi",
                    error = ex.Message
                });
            }
        }
        public class RoadmapDetailDto
        {
            public Guid RoadmapDetailId { get; set; }
            public Guid CourseId { get; set; }
            public string CourseName { get; set; }
            public string CourseDescription { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        [HttpGet("roadmap-details/{learnerLanguageId:guid}")]
        public async Task<IActionResult> GetRoadmapDetails(Guid learnerLanguageId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Kiểm tra quyền sở hữu
                var learnerLanguage = await _unitOfWork.LearnerLanguages.GetByIdAsync(learnerLanguageId);
                if (learnerLanguage == null || learnerLanguage.UserId != userId)
                    return Unauthorized(new { success = false, message = "Bạn không có quyền truy cập roadmap này" });

                // Lấy roadmap
                var allRoadmaps = await _unitOfWork.Roadmaps.GetAllAsync();
                var roadmap = allRoadmaps.FirstOrDefault(r => r.LearnerLanguageId == learnerLanguageId);
                if (roadmap == null)
                    return NotFound(new { success = false, message = "Không tìm thấy roadmap cho ngôn ngữ này" });

                // Lấy chi tiết roadmap
                var allRoadmapDetails = await _unitOfWork.RoadmapDetails.GetAllAsync();
                var roadmapDetails = allRoadmapDetails
                    .Where(rd => rd.RoadmapID == roadmap.RoadmapID)
                    .ToList();

                // Lấy thông tin khóa học cho từng roadmap detail
                var courseList = await _unitOfWork.Courses.GetAllAsync();
                var detailDtos = roadmapDetails.Select(rd =>
                {
                    var course = courseList.FirstOrDefault(c => c.CourseID == rd.CourseId);
                    return new RoadmapDetailDto
                    {
                        RoadmapDetailId = rd.RoadmapDetailID,
                        CourseId = rd.CourseId,
                        CourseName = course?.Title ?? "",
                        CourseDescription = course?.Description ?? "",
                        CreatedAt = rd.CreatedAt
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy chi tiết roadmap thành công",
                    data = new
                    {
                        learnerLanguageId,
                        roadmapId = roadmap.RoadmapID,
                        details = detailDtos
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap details for {LearnerLanguageId}", learnerLanguageId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy chi tiết roadmap" });
            }
        }
    }
}
