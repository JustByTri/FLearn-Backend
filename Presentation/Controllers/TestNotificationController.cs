using BLL.Services.FirebaseService;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/test-noti")]
    [ApiController]
    public class TestNotificationController : ControllerBase
    {
        private readonly FirebaseNotificationService _notificationService;

        public TestNotificationController(FirebaseNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> TestSend([FromBody] string token)
        {
            // Gọi trực tiếp, không qua Hangfire, không qua logic chấm điểm
            try
            {
                var data = new Dictionary<string, string> { { "type", "test" } };
                await _notificationService.SendNotificationAsync(token, "Test Title", "Test Body", data);
                return Ok("Đã gọi hàm gửi. Hãy check Console Log của Backend xem kết quả.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi ném ra từ hàm gửi: {ex.Message}");
            }
        }
    }
}
