using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Presentation.Controllers.Payment
{
 [ApiController]
 [AllowAnonymous]
[Route("api/payos")]
    public class PayOsRedirectController : ControllerBase
 {
 private readonly IConfiguration _configuration;
 private readonly ILogger<PayOsRedirectController> _logger;

 public PayOsRedirectController(IConfiguration configuration, ILogger<PayOsRedirectController> logger)
 {
 _configuration = configuration;
 _logger = logger;
 }


 [HttpGet("return")]
 public IActionResult Return()
 {
 return BuildDeepLinkResult("PAID");
 }


 [HttpGet("cancel")]
 public IActionResult Cancel()
 {
 return BuildDeepLinkResult("CANCELLED");
 }

 private IActionResult BuildDeepLinkResult(string defaultStatus)
 {
 try
 {
 var status = Request.Query["status"].FirstOrDefault() ?? defaultStatus;
 var orderCode = Request.Query["orderCode"].FirstOrDefault() ?? string.Empty;
 var amount = Request.Query["amount"].FirstOrDefault() ?? string.Empty;

 var appScheme = _configuration["MobileDeepLink:Scheme"] ?? "flearn"; // e.g., flearn
 var appPath = _configuration["MobileDeepLink:Path"] ?? "payment-result"; // e.g., payment-result

 var deepLink = $"{appScheme}://{appPath}?status={Uri.EscapeDataString(status)}&orderCode={Uri.EscapeDataString(orderCode)}&amount={Uri.EscapeDataString(amount)}";
 var fallbackUrl = _configuration["PaymentOSCallBack:ReturnUrl"] ?? "https://f-learn.app/waiting-checkout";

 var html = BuildRedirectHtml(deepLink, fallbackUrl);
 return new ContentResult
 {
 Content = html,
 ContentType = "text/html; charset=utf-8",
 StatusCode =200
 };
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error building PayOS redirect deep link");
 return Redirect(_configuration["PaymentOSCallBack:ReturnUrl"] ?? "/");
 }
 }

 private static string BuildRedirectHtml(string deepLink, string fallbackUrl)
 {
 // Safe encode for HTML and JS contexts
 string deepLinkHtml = WebUtility.HtmlEncode(deepLink);
 string fallbackHtml = WebUtility.HtmlEncode(fallbackUrl);
 string deepLinkJs = JsString(deepLink);
 string fallbackJs = JsString(fallbackUrl);

 var sb = new System.Text.StringBuilder();
 sb.Append("<!DOCTYPE html><html lang=\"en\"><head>");
 sb.Append("<meta charset=\"utf-8\" />");
 sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
 sb.Append("<title>Returning to app...</title>");
 sb.Append("<meta http-equiv=\"refresh\" content=\"0;url=").Append(deepLinkHtml).Append("\" />");
 sb.Append("<style>body{font-family:Arial,sans-serif;padding:24px;} .btn{display:inline-block;padding:12px16px;background:#3b82f6;color:#fff;text-decoration:none;border-radius:8px;}</style>");
 sb.Append("<script>(function(){ try{ window.location.href = '").Append(deepLinkJs).Append("'; }catch(e){} setTimeout(function(){ window.location.href = '").Append(fallbackJs).Append("'; },1200);})();</script>");
 sb.Append("</head><body>");
 sb.Append("<h2>Đang chuyển về ứng dụng...</h2>");
 sb.Append("<p>Nếu không tự động chuyển, bấm vào nút bên dưới:</p>");
 sb.Append("<p><a class=\"btn\" href=\"").Append(deepLinkHtml).Append("\">Mở ứng dụng</a></p>");
 sb.Append("<p>Hoặc quay về trang web: <a href=\"").Append(fallbackHtml).Append("\">").Append(fallbackHtml).Append("</a></p>");
 sb.Append("</body></html>");
 return sb.ToString();
 }

 private static string JsString(string s)
 {
 return (s ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'");
 }
 }
}
