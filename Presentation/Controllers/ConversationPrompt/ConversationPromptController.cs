using BLL.IServices.Admin;
using Common.DTO.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.ConversationPrompt
{
    [Route("api/admin/conversation-prompts")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ConversationPromptController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public ConversationPromptController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// Lấy danh sách tất cả global conversation prompts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllGlobalPrompts()
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var prompts = await _adminService.GetAllGlobalPromptsAsync(adminUserId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách conversation prompts thành công",
                    data = prompts,
                    total = prompts.Count,
                    statistics = new
                    {
                        totalPrompts = prompts.Count,
                        activePrompts = prompts.Count(p => p.IsActive),
                        defaultPrompt = prompts.FirstOrDefault(p => p.IsDefault)?.PromptName,
                        totalUsage = prompts.Sum(p => p.UsageCount)
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách prompts",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một global prompt
        /// </summary>
        [HttpGet("{promptId:guid}")]
        public async Task<IActionResult> GetGlobalPromptById(Guid promptId)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var prompt = await _adminService.GetGlobalPromptByIdAsync(adminUserId, promptId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin prompt thành công",
                    data = prompt
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
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
                    message = "Đã xảy ra lỗi khi lấy thông tin prompt",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo global conversation prompt mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateGlobalPrompt([FromBody] CreateGlobalPromptDto createPromptDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var newPrompt = await _adminService.CreateGlobalPromptAsync(adminUserId, createPromptDto);

                return CreatedAtAction(nameof(GetGlobalPromptById), new { promptId = newPrompt.GlobalPromptID }, new
                {
                    success = true,
                    message = "Tạo conversation prompt thành công",
                    data = newPrompt
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi tạo prompt",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật global conversation prompt
        /// </summary>
        [HttpPut("{promptId:guid}")]
        public async Task<IActionResult> UpdateGlobalPrompt(Guid promptId, [FromBody] UpdateGlobalPromptDto updatePromptDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var updatedPrompt = await _adminService.UpdateGlobalPromptAsync(adminUserId, promptId, updatePromptDto);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật prompt thành công",
                    data = updatedPrompt
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
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
                    message = "Đã xảy ra lỗi khi cập nhật prompt",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa global conversation prompt
        /// </summary>
        [HttpDelete("{promptId:guid}")]
        public async Task<IActionResult> DeleteGlobalPrompt(Guid promptId)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _adminService.DeleteGlobalPromptAsync(adminUserId, promptId);

                return Ok(new
                {
                    success = true,
                    message = "Xóa prompt thành công"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
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
                    message = "Đã xảy ra lỗi khi xóa prompt",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái global prompt
        /// </summary>
        [HttpPatch("{promptId:guid}/toggle-status")]
        public async Task<IActionResult> ToggleGlobalPromptStatus(Guid promptId)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _adminService.ToggleGlobalPromptStatusAsync(adminUserId, promptId);

                return Ok(new
                {
                    success = true,
                    message = "Thay đổi trạng thái prompt thành công"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
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
                    message = "Đã xảy ra lỗi khi thay đổi trạng thái prompt",
                    error = ex.Message
                });
            }
        }



        /// <summary>
        /// Preview prompt với sample data
        /// </summary>
        [HttpPost("{promptId:guid}/preview")]
        public async Task<IActionResult> PreviewPrompt(Guid promptId, [FromBody] PreviewPromptRequestDto previewRequest)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var prompt = await _adminService.GetGlobalPromptByIdAsync(adminUserId, promptId);

                if (prompt == null)
                {
                    return NotFound(new { success = false, message = "Prompt không tồn tại" });
                }

                // Tạo preview với sample data
                var previewContent = prompt.MasterPromptTemplate
                    .Replace("{LANGUAGE}", previewRequest.Language ?? "English")
                    .Replace("{LANGUAGE_CODE}", previewRequest.LanguageCode ?? "EN")
                    .Replace("{TOPIC}", previewRequest.Topic ?? "Casual Conversation")
                    .Replace("{TOPIC_DESCRIPTION}", previewRequest.TopicDescription ?? "General conversation practice")
                    .Replace("{DIFFICULTY_LEVEL}", previewRequest.DifficultyLevel ?? "Intermediate")
                    .Replace("{GENERATED_SCENARIO}", previewRequest.SampleScenario ?? "You are practicing conversation in a casual setting")
                    .Replace("{ROLEPLAY_CHARACTER}", previewRequest.SampleRole ?? "Friendly Conversation Partner");

                return Ok(new
                {
                    success = true,
                    message = "Preview prompt thành công",
                    data = new
                    {
                        promptId,
                        promptName = prompt.PromptName,
                        previewContent,
                        sampleData = previewRequest,
                        originalTemplate = prompt.MasterPromptTemplate
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi preview prompt",
                    error = ex.Message
                });
            }
        }


        /// <summary>
        /// Activate một prompt (chỉ 1 active tại 1 thời điểm)
        /// </summary>
        [HttpPost("{promptId:guid}/activate")]
        public async Task<IActionResult> ActivateGlobalPrompt(Guid promptId)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var activatedPrompt = await _adminService.ActivateGlobalPromptAsync(adminUserId, promptId);

                return Ok(new
                {
                    success = true,
                    message = "Prompt activated successfully. Previous active prompt has been archived.",
                    data = activatedPrompt,
                    note = "This prompt is now active and will be used for all new conversations"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error activating prompt",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy prompt đang active
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveGlobalPrompt()
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var activePrompt = await _adminService.GetActiveGlobalPromptAsync(adminUserId);

                return Ok(new
                {
                    success = true,
                    message = "Active prompt retrieved",
                    data = activePrompt,
                    note = "This is the prompt currently used for all new conversations"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        public class PreviewPromptRequestDto
        {
            public string Language { get; set; } = "English";
            public string LanguageCode { get; set; } = "EN";
            public string Topic { get; set; } = "Restaurant";
            public string TopicDescription { get; set; } = "Ordering food and drinks";
            public string DifficultyLevel { get; set; } = "B1";
            public string SampleScenario { get; set; } = "You are at a restaurant and want to order food";
            public string SampleRole { get; set; } = "Waiter";
        }
    }
}

