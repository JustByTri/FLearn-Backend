using BLL.IServices.Language;
using Common.DTO.ApiResponse;
using Common.DTO.Language;
using Common.DTO.Leaderboard;
using Common.DTO.Paging.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Presentation.Helpers;
namespace Presentation.Controllers.Language
{
    [Route("api/languages")]
    [ApiController]
    public class LanguageController : ControllerBase
    {
        private readonly ILanguageService _languageService;
        public LanguageController(ILanguageService languageService)
        {
            _languageService = languageService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _languageService.GetAllAsync();
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Lấy tất cả các cấp độ (A1, N5, HSK1...) theo một ngôn ngữ
        /// </summary>
        [HttpGet("{languageId:guid}/levels")]
        public async Task<IActionResult> GetLanguageLevels(Guid languageId)
        {
            try
            {
                var levels = await _languageService.GetLanguageLevelsAsync(languageId);
                return Ok(new { success = true, data = levels });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }
        [HttpGet("{langCode}/programs")]
        public async Task<IActionResult> GetProgramResponses([AllowedLang] string langCode, [FromQuery] PagingRequest pagingRequest)
        {
            var response = await _languageService.GetProgramResponsesAsync(langCode, pagingRequest);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Lấy bảng xếp hạng (leaderboard) theo ngôn ngữ (dựa trên StreakDays).
        /// </summary>s
        /// <param name="languageId">ID của ngôn ngữ.</param>
        /// <param name="count">Số lượng người dùng top đầu (mặc định 20, tối đa 100).</param>
        [AllowAnonymous] 
        [HttpGet("{languageId}/leaderboard")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<LeaderboardEntryDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetLeaderboard(
            [FromRoute] Guid languageId,
            [FromQuery][Range(1, 100)] int count = 20)
        {
            var response = await _languageService.GetLeaderboardByLanguageAsync(languageId, count);
            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thứ hạng và StreakDays hiện tại của người dùng đang đăng nhập cho một ngôn ngữ.
        /// </summary>
        /// <param name="languageId">ID của ngôn ngữ.</param>
        [Authorize(Roles = "Learner")]
        [HttpGet("{languageId}/my-rank")]
        [ProducesResponseType(typeof(BaseResponse<MyRankDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetMyRank([FromRoute] Guid languageId)
        {
            if (!this.TryGetUserId(out Guid userId, out var errorResult))
            {
                return errorResult!;
            }

            var response = await _languageService.GetMyRankAsync(languageId, userId);
            return StatusCode(response.Code, response);
        }
    }
}
