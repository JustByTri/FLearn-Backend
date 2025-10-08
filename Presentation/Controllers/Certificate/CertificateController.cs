using BLL.IServices.Certificate;
using Common.DTO.Certificate.Response;
using Common.DTO.Language;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Certificate
{
    [Route("api/certificates")]
    [ApiController]
    public class CertificateController : ControllerBase
    {
        private readonly ICertificateService _certificateService;
        public CertificateController(ICertificateService certificateService)
        {
            _certificateService = certificateService;
        }
        [HttpGet("by-lang")]
        public async Task<ActionResult<PagedResponse<IEnumerable<CertificateResponse>>>> GetCertificatesByLang(
            [AllowedLang] string lang,
            [FromQuery] PagingRequest request)
        {
            var result = await _certificateService.GetCertificatesByLang(lang, request);
            return Ok(result);
        }
    }
}
