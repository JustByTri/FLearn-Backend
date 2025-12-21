using BLL.IServices.AI;
using BLL.IServices.Assessment;
using Common.DTO.Assement;
using Common.DTO.Learner;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using BLL.IServices.Course; 
using BLL.IServices.Redis;
using Microsoft.EntityFrameworkCore;


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

        private readonly IRedisService _redisService;



        public VoiceAssessmentController(
            IVoiceAssessmentService voiceAssessmentService,
            IUnitOfWork unitOfWork,

            ILogger<VoiceAssessmentController> logger,
            IRedisService redisService)
        {
            _logger = logger;
            _voiceAssessmentService = voiceAssessmentService;
            _unitOfWork = unitOfWork;

            _redisService = redisService;
        }
        /// <summary>
        /// Lấy danh sách Khung chương trình (Programs) theo Ngôn ngữ.
        /// </summary>
        [HttpGet("programs/{languageId:guid}")]
        public async Task<IActionResult> GetProgramsByLanguage(Guid languageId)
        {
            try
            {

                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngôn ngữ." });
                }


                var allPrograms = await _unitOfWork.Programs.GetAllAsync();


                var programs = allPrograms
                    .Where(p => p.LanguageId == languageId && p.Status == true)
                    .Select(p => new
                    {

                        p.ProgramId,
                        p.Name,
                        p.Description
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = programs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy Programs theo LanguageId: {LanguageId}", languageId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi" });
            }
        }
        /// <summary>
        /// 1. Bắt đầu bài đánh giá (Theo Program)
        /// </summary>
        [HttpPost("start")] // Đổi route
        public async Task<IActionResult> StartProgramAssessment(
            [FromQuery] Guid languageId,
            [FromQuery] Guid programId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _logger.LogInformation("Bắt đầu Program Assessment: UserId={UserId}, Prog={ProgId}", userId, programId);

                var assessment = await _voiceAssessmentService.StartProgramAssessmentAsync(userId, languageId, programId);

                return Ok(new { success = true, message = "Bắt đầu thành công", data = assessment });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi bắt đầu program assessment");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// 2. Lấy câu hỏi hiện tại
        /// </summary>
        [HttpGet("{assessmentId:guid}/current-question")]
        public async Task<IActionResult> GetCurrentQuestion(Guid assessmentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (!await _voiceAssessmentService.ValidateAssessmentIdAsync(assessmentId, userId))
                    return Unauthorized(new { success = false, message = "Bạn không có quyền truy cập." });

                var question = await _voiceAssessmentService.GetCurrentQuestionAsync(assessmentId);
                return Ok(new { success = true, data = question });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 3. Nộp file audio hoặc bỏ qua
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
                if (!await _voiceAssessmentService.ValidateAssessmentIdAsync(assessmentId, userId))
                    return Unauthorized(new { success = false, message = "Bạn không có quyền truy cập." });

                if (!formDto.IsSkipped && formDto.AudioFile == null)
                    return BadRequest(new { success = false, message = "Cần gửi file âm thanh hoặc chọn bỏ qua" });

                var response = new VoiceAssessmentResponseDto
                {
                    AssessmentId = assessmentId,
                    QuestionNumber = formDto.QuestionNumber,
                    IsSkipped = formDto.IsSkipped,
                    AudioFile = formDto.AudioFile
                };

                await _voiceAssessmentService.SubmitVoiceResponseAsync(assessmentId, response);

                var assessment = await _voiceAssessmentService.RestoreAssessmentFromIdAsync(assessmentId);
                var isCompleted = assessment?.CurrentQuestionIndex >= assessment?.Questions.Count;

                return Ok(new
                {
                    success = true,
                    message = formDto.IsSkipped ? "Đã bỏ qua" : "Đã lưu",
                    data = new { isCompleted, nextQuestionIndex = assessment?.CurrentQuestionIndex }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nộp bài");
                return BadRequest(new { success = false, ex.Message });
            }
        }

        /// <summary>
        /// 4. Hoàn thành bài đánh giá
        /// </summary>
        [HttpPost("complete/{assessmentId:guid}")]
        public async Task<IActionResult> CompleteAssessment(Guid assessmentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (!await _voiceAssessmentService.ValidateAssessmentIdAsync(assessmentId, userId))
                    return Unauthorized(new { success = false, message = "Bạn không có quyền truy cập." });

                _logger.LogInformation("User {UserId} hoàn thành assessment {AssessmentId}", userId, assessmentId);


                var result = await _voiceAssessmentService.CompleteProgramAssessmentAsync(assessmentId);

                var message = (result.RecommendedCourses?.Any() == true)
                    ? $"Hoàn thành! Tìm thấy {result.RecommendedCourses.Count} khóa học cho trình độ {result.DeterminedLevel}."
                    : $"Hoàn thành! Hiện chưa có khóa học cho trình độ {result.DeterminedLevel}.";

                return Ok(new
                {
                    success = true,
                    message = message,
                    data = result // Trả về VoiceAssessmentResultDto (đã chứa LearnerLanguageId)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hoàn thành assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new { success = false, message = "Lỗi khi hoàn thành", error = ex.Message });
            }
        }

        /// <summary>
        /// 5. Chấp nhận kết quả (Lưu Level)
        /// </summary>
        [HttpPost("accept-assessment")] // Đổi route
        public async Task<IActionResult> AcceptAssessmentResult([FromBody] AcceptVoiceAssessmentRequestDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var learnerLanguage = await _unitOfWork.LearnerLanguages.GetByIdAsync(request.LearnerLanguageId);
                if (learnerLanguage == null || learnerLanguage.UserId != userId)
                {
                    return Unauthorized(new { success = false, message = "Bạn không có quyền." });
                }

                // ✅ SỬA LỖI: Gọi hàm Get MỚI (dùng LearnerLanguageId)
                var result = await _voiceAssessmentService.GetAssessmentResultAsync(request.LearnerLanguageId);
                if (result == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy kết quả để chấp nhận." });

                // ✅ SỬA LỖI: Gọi hàm Accept MỚI
                await _voiceAssessmentService.AcceptAssessmentAsync(request.LearnerLanguageId);

                // Cập nhật Ngôn ngữ hoạt động
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user != null)
                {
                    user.ActiveLanguageId = learnerLanguage.LanguageId;
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = "Đã chấp nhận và lưu trình độ của bạn.",
                    data = new
                    {
                        learnerLanguageId = request.LearnerLanguageId,
                        determinedLevel = result.DeterminedLevel,
                        roadmapCreated = false // ✅ Theo yêu cầu
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấp nhận assessment");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        /// <summary>
        /// 6. Từ chối kết quả (Xóa Redis)
        /// </summary>
        [HttpPost("reject-assessment")] // Đổi route
        public async Task<IActionResult> RejectAssessmentResult([FromBody] AcceptVoiceAssessmentRequestDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var learnerLanguage = await _unitOfWork.LearnerLanguages.GetByIdAsync(request.LearnerLanguageId);
                if (learnerLanguage == null || learnerLanguage.UserId != userId)
                {
                    return Unauthorized(new { success = false, message = "Bạn không có quyền." });
                }

                // ✅ SỬA LỖI: Gọi hàm Reject MỚI
                await _voiceAssessmentService.RejectAssessmentAsync(request.LearnerLanguageId);

                return Ok(new
                {
                    success = true,
                    message = "Đã từ chối kết quả. Bạn có thể làm lại."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi từ chối assessment");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        /// <summary>
        /// Thay đổi ngôn ngữ học tập. Kiểm tra xem đã làm test cho ngôn ngữ mới chưa.
        /// </summary>
        [HttpPost("switch-language/{languageId:guid}")]
        public async Task<IActionResult> SwitchActiveLanguage(Guid languageId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

              
                var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
                if (language == null)
                {
                    return NotFound(new { success = false, message = "Ngôn ngữ không tồn tại." });
                }
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) return Unauthorized();

              
                var allLearnerLangs = await _unitOfWork.LearnerLanguages.GetAllAsync();
                var learnerLanguage = allLearnerLangs.FirstOrDefault(ll => ll.UserId == userId && ll.LanguageId == languageId);

              
                if (learnerLanguage == null)
                {
                    _logger.LogWarning("Switch failed: User {UserId} chưa có LearnerLanguage cho {LanguageId}", userId, languageId);
                    return Ok(new
                    {
                        success = true,
                        action = "REQUIRE_ASSESSMENT",
                        message = $"Bạn cần hoàn thành bài đánh giá đầu vào cho {language.LanguageName}."
                    });
                }

          
                if (learnerLanguage.ProficiencyLevel == "Pending Assessment")
                {
                    _logger.LogWarning("Switch failed: User {UserId} có {LanguageId} đang 'Pending Assessment'", userId, languageId);

                    var pendingResult = await _voiceAssessmentService.GetAssessmentResultAsync(learnerLanguage.LearnerLanguageId);
                    if (pendingResult != null)
                    {
                      
                        return Ok(new
                        {
                            success = true,
                            action = "REVIEW_PENDING_ASSESSMENT", 
                            message = $"Bạn có kết quả đánh giá cho {language.LanguageName} đang chờ chấp nhận.",
                            data = pendingResult 
                        });
                    }

                   
                    return Ok(new
                    {
                        success = true,
                        action = "REQUIRE_ASSESSMENT", 
                        message = $"Bạn chưa hoàn thành bài đánh giá cho {language.LanguageName}."
                    });
                }

      

                _logger.LogInformation("Switch success: User {UserId} đổi ngôn ngữ sang {LanguageId}", userId, languageId);

         
                user.ActiveLanguageId = languageId;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(); 

                return Ok(new
                {
                    success = true,
                    action = "PROCEED_TO_HOME",
                    message = $"Chào mừng trở lại! Bạn đã sẵn sàng học {language.LanguageName}.",
                    data = new
                    {
                        learnerLanguage.LearnerLanguageId,
                        learnerLanguage.ProficiencyLevel
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đổi ngôn ngữ cho User");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        /// <summary>
        /// Trả về trạng thái profile cho learner.
        /// </summary>
        [HttpGet("profile/status")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetProfileStatus()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "User not found" });
            var learnerLang = await _unitOfWork.LearnerLanguages.GetQuery()
                .Where(ll => ll.UserId == userId && ll.LanguageId == user.ActiveLanguageId)
                .FirstOrDefaultAsync();
            var activeLanguage = user.ActiveLanguageId.HasValue
                ? await _unitOfWork.Languages.GetByIdAsync(user.ActiveLanguageId.Value)
                : null;
            var isDoingAssessment = false;
            string? assessmentStep = null;
            if (learnerLang == null || learnerLang.ProficiencyLevel == "Pending Assessment")
            {
                isDoingAssessment = true;
                assessmentStep = "Not Assessment";
            }
            var result = new Common.DTO.Learner.ProfileStatusDto
            {
                UserId = userId,
                ActiveLanguageId = user.ActiveLanguageId,
                ActiveLanguageName = activeLanguage?.LanguageName,
                IsDoingAssessment = isDoingAssessment,
                AssessmentStep = assessmentStep
            };
            return Ok(new { success = true, data = result });
        }
    }
}

