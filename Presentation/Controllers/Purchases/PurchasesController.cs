using BLL.IServices.Payment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Refund.Request;
using DAL.Helpers;
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
        [Authorize(Roles = "Learner")]
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
        [Authorize(Roles = "Learner")]
        [HttpGet("purchases")]
        public async Task<IActionResult> GetUserPurchaseDetails([FromQuery] PagingRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _purchaseService.GetPurchaseDetailsByUserIdAsync(userId, request);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("purchases/{purchaseId:guid}/details")]
        public async Task<IActionResult> GetPurchaseById(Guid purchaseId)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _purchaseService.GetPurchaseByIdAsync(purchaseId, userId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpPost("purchases/{purchaseId}/payments")]
        public async Task<IActionResult> CreatePayment(Guid purchaseId)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _purchaseService.CreatePaymentForPurchaseAsync(userId, purchaseId);

            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpPost("payments/refund")]
        public async Task<IActionResult> CreateRefundRequest([FromBody] CreateRefundRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _purchaseService.CreateRefundRequestAsync(userId, request);
            return StatusCode(result.Code, result);
        }
        [HttpPost("payments/payos-callback")]
        public async Task<IActionResult> PayOSCallback([FromBody] PayOSWebhookBody payOSWebhookBody)
        {
            var result = await _paymentService.HandleCallbackAsync(payOSWebhookBody);
            return StatusCode(result.Code, result);
        }
        [HttpGet("payments/return-url")]
        public ContentResult PaymentReturn()
        {
            var code = Request.Query["code"].ToString();
            var id = Request.Query["id"].ToString();
            var cancel = Request.Query["cancel"].ToString();
            var status = Request.Query["status"].ToString();
            var orderCode = Request.Query["orderCode"].ToString();

            bool isSuccess = code == "00" && cancel == "false" && status == "PAID";
            bool isCancelled = cancel == "true" || status == "CANCELLED";
            bool isPending = status == "PENDING" || status == "PROCESSING";

            string htmlContent;

            if (isSuccess)
            {
                htmlContent = GenerateSuccessPage(orderCode, id);
            }
            else if (isCancelled)
            {
                htmlContent = GenerateCancelledPage(orderCode, status);
            }
            else if (isPending)
            {
                htmlContent = GeneratePendingPage(orderCode, status);
            }
            else
            {
                htmlContent = GenerateErrorPage(orderCode, code, status);
            }

            return new ContentResult
            {
                ContentType = "text/html",
                Content = htmlContent,
                StatusCode = 200
            };
        }
        #region HTML CONTENT
        private string GenerateSuccessPage(string orderCode, string paymentId)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Thanh toán thành công</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        margin: 0;
                        padding: 0;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                        color: #333;
                    }}
                    .container {{
                        background: white;
                        padding: 40px;
                        border-radius: 15px;
                        box-shadow: 0 15px 35px rgba(0,0,0,0.2);
                        text-align: center;
                        max-width: 500px;
                        width: 90%;
                    }}
                    .success-icon {{
                        color: #10B981;
                        font-size: 64px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        color: #059669;
                        font-size: 28px;
                        font-weight: bold;
                        margin-bottom: 15px;
                    }}
                    .info-box {{
                        background: #F3F4F6;
                        padding: 20px;
                        border-radius: 8px;
                        margin: 25px 0;
                        text-align: left;
                    }}
                    .info-item {{
                        margin-bottom: 10px;
                        display: flex;
                        justify-content: space-between;
                    }}
                    .info-label {{
                        font-weight: bold;
                        color: #374151;
                    }}
                    .info-value {{
                        color: #059669;
                    }}
                    .message {{
                        color: #374151;
                        font-size: 16px;
                        margin-bottom: 20px;
                        line-height: 1.6;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='success-icon'>✓</div>
                    <h1 class='title'>Thanh toán thành công!</h1>
                    <p class='message'>Cảm ơn bạn đã thanh toán. Giao dịch của bạn đã được xử lý thành công.</p>
        
                    <div class='info-box'>
                        <div class='info-item'>
                            <span class='info-label'>Mã đơn hàng:</span>
                            <span class='info-value'>{orderCode}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Mã giao dịch:</span>
                            <span class='info-value'>{paymentId}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Trạng thái:</span>
                            <span class='info-value'>Đã thanh toán</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Thời gian:</span>
                            <span class='info-value'>{TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</span>
                        </div>
                    </div>
        
                    <p class='message'>Bạn có thể đóng trang này. Thông tin đơn hàng đã được ghi nhận.</p>
                </div>
            </body>
            </html>";
        }
        private string GenerateCancelledPage(string orderCode, string status)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Đã hủy thanh toán</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
                        margin: 0;
                        padding: 0;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                        color: #333;
                    }}
                    .container {{
                        background: white;
                        padding: 40px;
                        border-radius: 15px;
                        box-shadow: 0 15px 35px rgba(0,0,0,0.2);
                        text-align: center;
                        max-width: 500px;
                        width: 90%;
                    }}
                    .cancel-icon {{
                        color: #EF4444;
                        font-size: 64px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        color: #DC2626;
                        font-size: 28px;
                        font-weight: bold;
                        margin-bottom: 15px;
                    }}
                    .info-box {{
                        background: #FEF2F2;
                        padding: 20px;
                        border-radius: 8px;
                        margin: 25px 0;
                        text-align: left;
                    }}
                    .info-item {{
                        margin-bottom: 10px;
                        display: flex;
                        justify-content: space-between;
                    }}
                    .info-label {{
                        font-weight: bold;
                        color: #374151;
                    }}
                    .info-value {{
                        color: #DC2626;
                    }}
                    .message {{
                        color: #374151;
                        font-size: 16px;
                        margin-bottom: 20px;
                        line-height: 1.6;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='cancel-icon'>✕</div>
                    <h1 class='title'>Đã hủy thanh toán</h1>
                    <p class='message'>Bạn đã hủy bỏ quá trình thanh toán.</p>
        
                    <div class='info-box'>
                        <div class='info-item'>
                            <span class='info-label'>Mã đơn hàng:</span>
                            <span class='info-value'>{orderCode}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Trạng thái:</span>
                            <span class='info-value'>{GetStatusText(status)}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Thời gian:</span>
                            <span class='info-value'>{TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</span>
                        </div>
                    </div>
        
                    <p class='message'>Bạn có thể đóng trang này.</p>
                </div>
            </body>
            </html>";
        }
        private string GeneratePendingPage(string orderCode, string status)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Đang xử lý</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
                        margin: 0;
                        padding: 0;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                        color: #333;
                    }}
                    .container {{
                        background: white;
                        padding: 40px;
                        border-radius: 15px;
                        box-shadow: 0 15px 35px rgba(0,0,0,0.2);
                        text-align: center;
                        max-width: 500px;
                        width: 90%;
                    }}
                    .pending-icon {{
                        color: #3B82F6;
                        font-size: 64px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        color: #1D4ED8;
                        font-size: 28px;
                        font-weight: bold;
                        margin-bottom: 15px;
                    }}
                    .info-box {{
                        background: #DBEAFE;
                        padding: 20px;
                        border-radius: 8px;
                        margin: 25px 0;
                        text-align: left;
                    }}
                    .info-item {{
                        margin-bottom: 10px;
                        display: flex;
                        justify-content: space-between;
                    }}
                    .info-label {{
                        font-weight: bold;
                        color: #374151;
                    }}
                    .info-value {{
                        color: #1D4ED8;
                    }}
                    .message {{
                        color: #374151;
                        font-size: 16px;
                        margin-bottom: 20px;
                        line-height: 1.6;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='pending-icon'>⏳</div>
                    <h1 class='title'>Đang xử lý</h1>
                    <p class='message'>Giao dịch của bạn đang được xử lý. Vui lòng chờ trong giây lát.</p>
        
                    <div class='info-box'>
                        <div class='info-item'>
                            <span class='info-label'>Mã đơn hàng:</span>
                            <span class='info-value'>{orderCode}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Trạng thái:</span>
                            <span class='info-value'>{GetStatusText(status)}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Thời gian:</span>
                            <span class='info-value'>{TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</span>
                        </div>
                    </div>
        
                    <p class='message'>Vui lòng không đóng trang này. Hệ thống sẽ tự động cập nhật.</p>
                </div>
            </body>
            </html>";
        }
        private string GenerateErrorPage(string orderCode, string errorCode, string status)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Lỗi thanh toán</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
                        margin: 0;
                        padding: 0;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                        color: #333;
                    }}
                    .container {{
                        background: white;
                        padding: 40px;
                        border-radius: 15px;
                        box-shadow: 0 15px 35px rgba(0,0,0,0.2);
                        text-align: center;
                        max-width: 500px;
                        width: 90%;
                    }}
                    .error-icon {{
                        color: #EF4444;
                        font-size: 64px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        color: #DC2626;
                        font-size: 28px;
                        font-weight: bold;
                        margin-bottom: 15px;
                    }}
                    .info-box {{
                        background: #FEF2F2;
                        padding: 20px;
                        border-radius: 8px;
                        margin: 25px 0;
                        text-align: left;
                    }}
                    .info-item {{
                        margin-bottom: 10px;
                        display: flex;
                        justify-content: space-between;
                    }}
                    .info-label {{
                        font-weight: bold;
                        color: #374151;
                    }}
                    .info-value {{
                        color: #DC2626;
                    }}
                    .message {{
                        color: #374151;
                        font-size: 16px;
                        margin-bottom: 20px;
                        line-height: 1.6;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='error-icon'>⚠️</div>
                    <h1 class='title'>Lỗi thanh toán</h1>
                    <p class='message'>Đã xảy ra lỗi trong quá trình thanh toán.</p>
        
                    <div class='info-box'>
                        <div class='info-item'>
                            <span class='info-label'>Mã đơn hàng:</span>
                            <span class='info-value'>{orderCode}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Mã lỗi:</span>
                            <span class='info-value'>{errorCode}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Trạng thái:</span>
                            <span class='info-value'>{GetStatusText(status)}</span>
                        </div>
                        <div class='info-item'>
                            <span class='info-label'>Thời gian:</span>
                            <span class='info-value'>{TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</span>
                        </div>
                    </div>
        
                    <p class='message'>Vui lòng thử lại sau hoặc liên hệ hỗ trợ nếu lỗi tiếp diễn.</p>
                </div>
            </body>
            </html>";
        }
        private string GetStatusText(string status)
        {
            return status switch
            {
                "PAID" => "Đã thanh toán",
                "PENDING" => "Chờ thanh toán",
                "PROCESSING" => "Đang xử lý",
                "CANCELLED" => "Đã hủy",
                _ => status
            };
        }
        #endregion
    }
}
