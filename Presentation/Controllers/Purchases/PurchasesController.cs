using BLL.IServices.Payment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Purchases
{
    [Route("api")]
    [ApiController]
    public class PurchasesController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PurchasesController> _logger;
        private readonly IConfiguration _configuration;
        public PurchasesController(IPurchaseService purchaseService, ILogger<PurchasesController> logger, IPaymentService paymentService, IConfiguration configuration)
        {
            _purchaseService = purchaseService;
            _logger = logger;
            _paymentService = paymentService;
            _configuration = configuration;
        }
        [Authorize]
        [HttpPost("purchases")]
        public async Task<IActionResult> PurchaseCourse([FromBody] PurchaseCourseRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail(ModelState));
            }

            var result = await _purchaseService.PurchaseCourseAsync(userId, request);

            return StatusCode(result.Code, result);
        }
        [Authorize]
        [HttpPost("purchases/{purchaseId}/payments")]
        public async Task<IActionResult> CreatePayment(Guid purchaseId)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail(ModelState));
            }

            var result = await _purchaseService.CreatePaymentForPurchaseAsync(userId, purchaseId);

            return StatusCode(result.Code, result);
        }
        [HttpPost("payments/payos-callback")]
        public async Task<IActionResult> PayOSCallback([FromBody] PayOSWebhookBody payOSWebhookBody)
        {
            var result = await _paymentService.HandleCallbackAsync(payOSWebhookBody);
            return StatusCode(result.Code, result);
        }
    }
}
