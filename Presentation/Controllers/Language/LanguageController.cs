using BLL.IServices.Language;
using Common.DTO.Language;
using Common.DTO.Paging.Request;
using Microsoft.AspNetCore.Mvc;

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
    }
}
