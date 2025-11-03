using Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Presentation.Controllers.Subscription
{
 [Route("api/subscriptions")]
 [ApiController]
 public class PlansController : ControllerBase
 {
 /// <summary>
 /// Danh sách gói l??t nói và giá hi?n t?i (hi?n th? VN?)
 /// </summary>
 [HttpGet("plans")]
 [AllowAnonymous]
 public IActionResult GetPlans()
 {
 var culture = new CultureInfo("vi-VN");
 var plans = SubscriptionConstants.SubscriptionQuotas
 .Where(kv => kv.Key != SubscriptionConstants.FREE)
 .Select(kv =>
 {
 var price = SubscriptionConstants.SubscriptionPrices.TryGetValue(kv.Key, out var p) ? p :0m;
 return new
 {
 plan = kv.Key,
 dailyQuota = kv.Value,
 price,
 priceVnd = string.Format(culture, "{0:C0}", price).Replace("?", "?")
 };
 })
 .OrderBy(x => x.dailyQuota)
 .ToList();

 return Ok(new { success = true, data = plans });
 }
 }
}
