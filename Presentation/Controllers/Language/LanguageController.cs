using BLL.IServices.Language;
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
    }
}
