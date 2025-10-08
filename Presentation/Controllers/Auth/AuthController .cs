using BLL.IServices.Auth;
using Common.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Presentation.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
                var username = User.FindFirstValue(ClaimTypes.Name);
                var email = User.FindFirstValue(ClaimTypes.Email);
                var createdAt = User.FindFirstValue("created_at");
                var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

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
                        roles = roles
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy thông tin người dùng" });
            }
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
    }
}


