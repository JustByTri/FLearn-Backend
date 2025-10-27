using BLL.IServices.Auth;
using BLL.IServices.Upload;
using Common.DTO.Auth;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Presentation.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        public AuthController(IAuthService authService, ICloudinaryService cloudinaryService, IUnitOfWork unitOfWork)
        {
            _authService = authService;
            _cloudinaryService = cloudinaryService;
            _unitOfWork = unitOfWork;
        }


        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] TempRegistrationDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                await _authService.RegisterAndSendOtpAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Đăng ký thành công! Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra và xác thực trong vòng 5 phút.",
                    email = request.Email
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại!" });
            }
        }


        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _authService.VerifyOtpAndCompleteRegistrationAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Xác thực thành công! Chào mừng bạn đến với Flearn. Email chào mừng đã được gửi.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi trong quá trình xác thực OTP. Vui lòng thử lại!" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _authService.LoginAsync(request);


                var userId = Guid.Parse(result.User.UserID.ToString());

                return Ok(new
                {
                    success = true,
                    message = "Đăng nhập thành công! Chào mừng bạn quay trở lại.",
                    data = result

                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại!" });
            }
        }


        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(new
                {
                    success = true,
                    message = "Làm mới token thành công",
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi trong quá trình làm mới token" });
            }
        }


        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var result = await _authService.LogoutAsync(request.RefreshToken);
                return Ok(new
                {
                    success = true,
                    message = "Đăng xuất thành công. Hẹn gặp lại bạn!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi trong quá trình đăng xuất" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

               
                var user = await _unitOfWork.Users.GetUserWithRolesAsync(Guid.Parse(userId));
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var username = user.UserName;
                var email = user.Email;
                var createdAt = user.CreatedAt;
                var roles = user.UserRoles?.Select(c => c.Role.Name).ToList() ?? new List<string>();
                var avatar = user.Avatar;
                var fullname = user.FullName;

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin người dùng thành công",
                    data = new
                    {
                        userId = userId,
                        username = username,
                        email = email,
                        createdAt = createdAt,
                        roles = roles,
                        avatar = avatar,
                        fullname = fullname,
                        dailyConversationLimit = user.DailyConversationLimit,
                        conversationsUsedToday = user.ConversationsUsedToday
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy thông tin người dùng" });
            }
        }
        [Authorize]
        [HttpPut("profile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileFormDto form)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            // Kiểm tra username đã tồn tại cho user khác chưa
            if (!string.Equals(user.UserName, form.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var isUsernameExists = await _unitOfWork.Users.IsUsernameExistsAsync(form.UserName);
                if (isUsernameExists)
                    return BadRequest(new { success = false, message = "Username đã tồn tại, vui lòng chọn tên khác." });
                user.UserName = form.UserName;
            }

            user.FullName = form.FullName;

            if (form.Avatar != null && form.Avatar.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(form.Avatar, "avatars");
                user.Avatar = uploadResult.Url;
            }

            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { success = true, message = "Profile updated successfully", data = new { user.Avatar, user.UserName } });
        }

        public class UpdateProfileFormDto
        {
            
            public string FullName { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
            public string UserName { get; set; }

            public IFormFile? Avatar { get; set; }
        }


        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
            }

            var result = await _authService.LoginGoogleAsync(request.IdToken);
            if (string.IsNullOrEmpty(result.AccessToken))
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi trong quá trình đăng nhập Google." });
            }

            return Ok(new { success = true, message = "Đăng nhập Google thành công", data = result });
        }


        /// <summary>
        /// Gửi lại mã OTP cho đăng ký
        /// </summary>
        [HttpPost("resend-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                await _authService.ResendOtpAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Mã OTP mới đã được gửi đến email của bạn. Vui lòng kiểm tra và xác thực trong vòng 5 phút.",
                    email = request.Email
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi gửi lại OTP. Vui lòng thử lại!" });
            }
        }

        /// <summary>
        /// Quên mật khẩu - gửi OTP reset password
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                await _authService.ForgotPasswordAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Nếu email/tài khoản tồn tại, mã OTP đặt lại mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra trong vòng 10 phút."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xử lý yêu cầu quên mật khẩu. Vui lòng thử lại!" });
            }
        }

        /// <summary>
        /// Đặt lại mật khẩu bằng OTP
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                await _authService.ResetPasswordAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại với mật khẩu mới."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại!" });
            }
        }
        /// <summary>
        /// Thay đổi mật khẩu của user hiện tại
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _authService.ChangePasswordAsync(userId, changePasswordDto);

                return Ok(new
                {
                    success = true,
                    message = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại với mật khẩu mới."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi đổi mật khẩu. Vui lòng thử lại!" });
            }
        }
    }
}


