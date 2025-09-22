using Common.DTO.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest);
        Task<AuthResponseDto> RegisterAsync(TempRegistrationDto registerRequest);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> RegisterAndSendOtpAsync(TempRegistrationDto registrationDto);
        Task<AuthResponseDto> VerifyOtpAndCompleteRegistrationAsync(VerifyOtpDto verifyOtpDto);
        Task<bool> ChangeStaffPasswordAsync(Guid adminUserId, ChangeStaffPasswordDto changePasswordDto);
     
        Task<bool> ResendOtpAsync(ResendOtpDto resendOtpDto);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    }



}

