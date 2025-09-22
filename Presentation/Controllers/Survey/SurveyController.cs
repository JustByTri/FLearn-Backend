using BLL.IServices.Survey;
using Common.DTO.Learner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Survey
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SurveyController : ControllerBase
    {
        private readonly IUserSurveyService _surveyService;

        public SurveyController(IUserSurveyService surveyService)
        {
            _surveyService = surveyService;
        }

        /// <summary>
        /// Kiểm tra xem user đã hoàn thành survey chưa
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetSurveyStatus()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var hasCompleted = await _surveyService.HasUserCompletedSurveyAsync(userId);

                UserSurveyResponseDto? survey = null;
                if (hasCompleted)
                {
                    survey = await _surveyService.GetUserSurveyAsync(userId);
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        hasCompletedSurvey = hasCompleted,
                        needsOnboarding = !hasCompleted,
                        survey = survey
                    },
                    message = hasCompleted ? "Đã hoàn thành khảo sát" : "Chưa hoàn thành khảo sát"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi kiểm tra trạng thái khảo sát",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy các lựa chọn cho form khảo sát
        /// </summary>
        /// <summary>
        /// Lấy các lựa chọn cho form khảo sát
        /// </summary>
        [HttpGet("options")]
        public async Task<IActionResult> GetSurveyOptions()
        {
            try
            {
                var options = new
                {
                    currentLevels = await _surveyService.GetCurrentLevelOptionsAsync(),
                    learningStyles = await _surveyService.GetLearningStyleOptionsAsync(),
                    prioritySkills = await _surveyService.GetPrioritySkillsOptionsAsync(),
                    targetTimelines = await _surveyService.GetTargetTimelineOptionsAsync(),

                  
                    speakingChallenges = await _surveyService.GetSpeakingChallengesOptionsAsync(),
                    preferredAccents = await _surveyService.GetPreferredAccentOptionsAsync(),

                    confidenceLevels = Enumerable.Range(1, 10).Select(i => new
                    {
                        value = i,
                        label = $"Mức {i}" + (i <= 3 ? " (Thấp)" : i <= 6 ? " (Trung bình)" : " (Cao)")
                    }).ToList()
                };

                return Ok(new
                {
                    success = true,
                    data = options,
                    message = "Lấy danh sách lựa chọn thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách lựa chọn",
                    error = ex.Message
                });
            }
        }
        /// <summary>
        /// Hoàn thành khảo sát lần đầu
        /// </summary>
        [HttpPost("complete")]
        public async Task<IActionResult> CompleteSurvey([FromBody] UserSurveyDto surveyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu khảo sát không hợp lệ",
                        errors = ModelState
                    });
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Check if already completed
                if (await _surveyService.HasUserCompletedSurveyAsync(userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn đã hoàn thành khảo sát trước đó"
                    });
                }

                var result = await _surveyService.CreateSurveyAsync(userId, surveyDto);

                return Ok(new
                {
                    success = true,
                    message = "Hoàn thành khảo sát thành công! AI đang phân tích và tạo gợi ý khóa học phù hợp cho bạn.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
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
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xử lý khảo sát",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin khảo sát và gợi ý khóa học
        /// </summary>
        [HttpGet("my-survey")]
        public async Task<IActionResult> GetMySurvey()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var survey = await _surveyService.GetUserSurveyAsync(userId);

                if (survey == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Bạn chưa hoàn thành khảo sát"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin khảo sát thành công",
                    data = survey
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thông tin khảo sát",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo lại gợi ý khóa học bằng AI
        /// </summary>
        [HttpPost("regenerate-recommendations")]
        public async Task<IActionResult> RegenerateRecommendations()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                if (!await _surveyService.HasUserCompletedSurveyAsync(userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn cần hoàn thành khảo sát trước"
                    });
                }

                var recommendations = await _surveyService.GenerateRecommendationsAsync(userId);

                return Ok(new
                {
                    success = true,
                    message = "Tạo gợi ý khóa học mới thành công",
                    data = recommendations
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi tạo gợi ý khóa học",
                    error = ex.Message
                });
            }
        }
    }
}

