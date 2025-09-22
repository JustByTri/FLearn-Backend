using Common.DTO.Auth;

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

        Task<AuthResponseDto> LoginGoogleAsync(string idToken);


    }



}

