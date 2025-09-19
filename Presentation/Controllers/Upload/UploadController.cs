using BLL.IServices.Upload;
using Common.DTO.Upload;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Upload
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class UploadController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]

        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string? folder = "general")
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng chọn file để upload"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userFolder = string.IsNullOrEmpty(folder) ? $"users/{userId}/images" : $"users/{userId}/{folder}";

                var result = await _cloudinaryService.UploadImageAsync(file, userFolder);

                return Ok(new
                {
                    success = true,
                    message = "Upload ảnh thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
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
                    message = "Đã xảy ra lỗi khi upload ảnh",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Upload một file tài liệu
        /// </summary>
        [HttpPost("document")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string? folder = "documents")
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng chọn file để upload"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userFolder = string.IsNullOrEmpty(folder) ? $"users/{userId}/documents" : $"users/{userId}/{folder}";

                var result = await _cloudinaryService.UploadDocumentAsync(file, userFolder);

                return Ok(new
                {
                    success = true,
                    message = "Upload tài liệu thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
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
                    message = "Đã xảy ra lỗi khi upload tài liệu",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Upload nhiều file cùng lúc
        /// </summary>
        [HttpPost("multiple")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMultipleFiles([FromForm] IList<IFormFile> files, [FromForm] string? folder = "general")
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng chọn ít nhất một file để upload"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userFolder = string.IsNullOrEmpty(folder) ? $"users/{userId}/files" : $"users/{userId}/{folder}";

                var results = await _cloudinaryService.UploadMultipleFilesAsync(files, userFolder);

                return Ok(new
                {
                    success = true,
                    message = $"Upload thành công {results.Count} file",
                    data = results,
                    totalUploaded = results.Count
                });
            }
            catch (ArgumentException ex)
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
                    message = "Đã xảy ra lỗi khi upload file",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Upload file tổng quát (tự động phát hiện loại file)
        /// </summary>
        [HttpPost("general")]
        public async Task<IActionResult> UploadGeneralFile([FromForm] IFormFile file, [FromForm] string? folder = "general")
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng chọn file để upload"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userFolder = string.IsNullOrEmpty(folder) ? $"users/{userId}/files" : $"users/{userId}/{folder}";

                // Determine if it's an image or document based on file extension
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                UploadResultDto result;

                if (imageExtensions.Contains(fileExtension))
                {
                    result = await _cloudinaryService.UploadImageAsync(file, userFolder);
                }
                else
                {
                    result = await _cloudinaryService.UploadDocumentAsync(file, userFolder);
                }

                return Ok(new
                {
                    success = true,
                    message = "Upload file thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
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
                    message = "Đã xảy ra lỗi khi upload file",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa file trên Cloudinary
        /// </summary>
        [HttpDelete("{publicId}")]
        public async Task<IActionResult> DeleteFile(string publicId)
        {
            try
            {
                // Decode publicId if it's URL encoded
                publicId = Uri.UnescapeDataString(publicId);

                var result = await _cloudinaryService.DeleteFileAsync(publicId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Xóa file thành công"
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Không thể xóa file. File có thể không tồn tại."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xóa file",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Upload avatar cho user
        /// </summary>
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng chọn ảnh avatar"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var avatarFolder = $"users/{userId}/avatar";

                var result = await _cloudinaryService.UploadImageAsync(file, avatarFolder);

                return Ok(new
                {
                    success = true,
                    message = "Upload avatar thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
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
                    message = "Đã xảy ra lỗi khi upload avatar",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin về giới hạn upload
        /// </summary>
        [HttpGet("limits")]
        public IActionResult GetUploadLimits()
        {
            return Ok(new
            {
                success = true,
                data = new
                {
                    maxImageSize = "5MB",
                    maxDocumentSize = "10MB",
                    maxFilesPerRequest = 10,
                    supportedImageFormats = new[] { "JPG", "JPEG", "PNG", "GIF", "WEBP", "BMP" },
                    supportedDocumentFormats = new[] { "PDF", "DOC", "DOCX", "TXT", "RTF", "XLS", "XLSX", "PPT", "PPTX" }
                },
                message = "Thông tin giới hạn upload"
            });
        }
    }
}
