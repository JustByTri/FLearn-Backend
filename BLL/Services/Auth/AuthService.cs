using BLL.IServices.Auth;
using BLL.Settings;
using Common.DTO.Auth;
using DAL.Models;
using DAL.UnitOfWork;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BLL.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;
        private readonly GoogleAuthSettings _googleAuthSettings;
        public AuthService(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings, IEmailService emailService, IOptions<GoogleAuthSettings> googleAuthSettings)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
            _googleAuthSettings = googleAuthSettings.Value;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest)
        {
            var user = await GetUserByEmailOrUsernameAsync(loginRequest.UsernameOrEmail);

            if (user == null || !user.Status)
                throw new UnauthorizedAccessException("Tài khoản của bạn không khả dụng, liên hệ với quản trị viên");

            if (!VerifyPassword(loginRequest.Password, user.PasswordHash, user.PasswordSalt))
                throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu của bạn không đúng");

            user.LastAcessAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);

           
            var refreshTokenExpirationDays = loginRequest.RememberMe ? 30 : _jwtSettings.RefreshTokenExpirationDays;

            var (accessToken, refreshToken) = await GenerateTokensAsync(user, refreshTokenExpirationDays);

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken.Token,
                AccessTokenExpires = accessToken.ExpiresAt,
                RefreshTokenExpires = refreshToken.ExpiresAt,
                User = MapToUserInfoDto(user),
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
            };
        }

    
        private async Task<(TokenInfo accessToken, RefreshToken refreshToken)> GenerateTokensAsync(User user, int? customRefreshTokenDays = null)
        {
            var accessToken = GenerateAccessToken(user);

            var refreshTokenDays = customRefreshTokenDays ?? _jwtSettings.RefreshTokenExpirationDays;

            var refreshToken = new RefreshToken
            {
                RefreshTokenID = Guid.NewGuid(),
                UserID = user.UserID,
                Token = GenerateRefreshToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
                IsRevoked = false
            };

            await _unitOfWork.RefreshTokens.CreateAsync(refreshToken);

            return (accessToken, refreshToken);
        }

        public async Task<bool> RegisterAndSendOtpAsync(TempRegistrationDto registrationDto)
        {
      
            var normalizedEmail = registrationDto.Email.Trim().ToLowerInvariant();

         
            if (await _unitOfWork.Users.IsEmailExistsAsync(normalizedEmail))
                throw new InvalidOperationException("Email đã được đăng ký bởi tài khoản khác, vui lòng dùng email khác");

            if (await _unitOfWork.Users.IsUsernameExistsAsync(registrationDto.UserName))
                throw new InvalidOperationException("Tên người dùng đã có người sử dụng, hãy thử tên khác nhé");

            var (passwordHash, passwordSalt) = CreatePasswordHash(registrationDto.Password);

       
            var otp = await GenerateSecureRegistrationOtpAsync(normalizedEmail);

   
            await _unitOfWork.TempRegistrations.InvalidateTempRegistrationsAsync(normalizedEmail);

            var tempRegistration = new TempRegistration
            {
                Email = normalizedEmail,
                UserName = registrationDto.UserName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                OtpCode = otp,
                ExpireAt = DateTime.UtcNow.AddMinutes(5)
            };
            await _unitOfWork.TempRegistrations.CreateAsync(tempRegistration);

            await _emailService.SendEmailConfirmationAsync(normalizedEmail, registrationDto.UserName, otp);
            return true;
        }

        private async Task<string> GenerateSecureRegistrationOtpAsync(string email)
        {
            string otpCode;
            bool isUnique;
            int maxAttempts = 10;
            int attempts = 0;

            do
            {
               
                using (var rng = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[4];
                    rng.GetBytes(bytes);
                    var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
                    otpCode = (randomNumber % 900000 + 100000).ToString();
                }

            
                var allTempRegs = await _unitOfWork.TempRegistrations.GetAllAsync();
                isUnique = !allTempRegs.Any(tr =>
                    tr.Email.ToLowerInvariant() == email.ToLowerInvariant() &&
                    tr.OtpCode == otpCode &&
                    !tr.IsUsed &&
                    tr.ExpireAt > DateTime.UtcNow);

                attempts++;
            }
            while (!isUnique && attempts < maxAttempts);

            return otpCode;
        }
        public async Task<AuthResponseDto> VerifyOtpAndCompleteRegistrationAsync(VerifyOtpDto verifyOtpDto)
        {

            var tempRegistration = await _unitOfWork.TempRegistrations.GetValidTempRegistrationAsync(verifyOtpDto.Email, verifyOtpDto.OtpCode);
            if (tempRegistration == null)
                throw new InvalidOperationException("OTP có vẻ như không đúng, hãy thử lại.");


            tempRegistration.IsUsed = true;
            await _unitOfWork.TempRegistrations.UpdateAsync(tempRegistration);


            if (await _unitOfWork.Users.IsEmailExistsAsync(tempRegistration.Email))
                throw new InvalidOperationException("Email đã được sử dụng trong quá trình xác thực OTP .");

            if (await _unitOfWork.Users.IsUsernameExistsAsync(tempRegistration.UserName))
                throw new InvalidOperationException("Tài khoản đã được sử dụng trong quá trình sử dụng OTP.");


            var user = new User
            {
                UserID = Guid.NewGuid(),
                UserName = tempRegistration.UserName,
                Email = tempRegistration.Email,
                PasswordHash = tempRegistration.PasswordHash,
                PasswordSalt = tempRegistration.PasswordSalt,
                JobTitle = "Learner",
                Interests = string.Empty,
                BirthDate = DateTime.Now.AddYears(-18),
                ProfilePictureUrl = string.Empty,
                Status = true,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                LastAcessAt = DateTime.UtcNow,
                IsEmailConfirmed = true,
                MfaEnabled = false,
                StreakDays = 0
            };
            await _unitOfWork.Users.CreateAsync(user);


            var defaultRole = await _unitOfWork.Roles.GetByNameAsync("Learner");
            if (defaultRole != null)
            {
                var userRole = new UserRole
                {
                    UserRoleID = Guid.NewGuid(),
                    UserID = user.UserID,
                    RoleID = defaultRole.RoleID
                };
                await _unitOfWork.UserRoles.CreateAsync(userRole);
            }


            await _emailService.SendWelcomeEmailAsync(user.Email, user.UserName);


            var (accessToken, refreshToken) = await GenerateTokensAsync(user);

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken.Token,
                AccessTokenExpires = accessToken.ExpiresAt,
                RefreshTokenExpires = refreshToken.ExpiresAt,
                User = MapToUserInfoDto(user),
                Roles = new List<string> { "Learner" }
            };
        }
        public async Task<AuthResponseDto> RegisterAsync(TempRegistrationDto registerRequest)
        {

            throw new NotImplementedException("Hãy xác thực đăng kí với OTP.");
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var refreshTokenEntity = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);

            if (refreshTokenEntity == null || refreshTokenEntity.IsRevoked || refreshTokenEntity.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token không khả dụng hoặc hết hạn");

            var user = await _unitOfWork.Users.GetUserWithRolesAsync(refreshTokenEntity.UserID);
            if (user == null || !user.Status)
                throw new UnauthorizedAccessException("Người dùng không tìm thấy hoặc ngưng hoạt động");


            refreshTokenEntity.IsRevoked = true;
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(refreshTokenEntity);


            var (accessToken, newRefreshToken) = await GenerateTokensAsync(user);

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshToken.Token,
                AccessTokenExpires = accessToken.ExpiresAt,
                RefreshTokenExpires = newRefreshToken.ExpiresAt,
                User = MapToUserInfoDto(user),
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
            };
        }
        private DateTime ConvertToVietnamTime(DateTime utcTime)
        {
            TimeZoneInfo vietnamZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, vietnamZone);
        }
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            return await _unitOfWork.RefreshTokens.RevokeTokenAsync(refreshToken);
        }

        #region Private Methods

        private async Task<User> GetUserByEmailOrUsernameAsync(string emailOrUsername)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(emailOrUsername);
            if (user == null)
                user = await _unitOfWork.Users.GetByUsernameAsync(emailOrUsername);
            return user;
        }

        private async Task<(TokenInfo accessToken, RefreshToken refreshToken)> GenerateTokensAsync(User user)
        {
            var accessToken = GenerateAccessToken(user);

            var refreshToken = new RefreshToken
            {
                RefreshTokenID = Guid.NewGuid(),
                UserID = user.UserID,
                Token = GenerateRefreshToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            await _unitOfWork.RefreshTokens.CreateAsync(refreshToken);

            return (accessToken, refreshToken);
        }

        private TokenInfo GenerateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),

        new Claim("user_id", user.UserID.ToString()),
        new Claim("username", user.UserName),
        new Claim("email", user.Email),

        new Claim("created_at", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
    };

            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }
            }

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials
            );

       
            var expiresAtVietnam = TimeZoneInfo.ConvertTimeFromUtc(expiresAtUtc,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            return new TokenInfo
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAtUtc
            };
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }

        private (string hash, string salt) CreatePasswordHash(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                var salt = Convert.ToBase64String(hmac.Key);
                var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
                return (hash, salt);
            }
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var hmac = new HMACSHA512(saltBytes))
            {
                var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
                return computedHash == hash;
            }
        }

        private UserInfoDto MapToUserInfoDto(User user)
        {
            return new UserInfoDto
            {
                UserID = user.UserID,
                UserName = user.UserName,
                Email = user.Email,
                IsEmailConfirmed = user.IsEmailConfirmed,
                CreatedAt = user.CreatedAt,
                LastAccessAt = user.LastAcessAt
            };
        }
        public async Task<bool> ChangeStaffPasswordAsync(Guid adminUserId, ChangeStaffPasswordDto changePasswordDto)
        {

            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể đổi mật khẩu staff");
            }


            var staffUser = await _unitOfWork.Users.GetUserWithRolesAsync(changePasswordDto.StaffUserId);
            if (staffUser == null || !staffUser.UserRoles.Any(ur => ur.Role.Name == "Staff"))
            {
                throw new InvalidOperationException("Người dùng được chọn không phải là staff");
            }


            var (newPasswordHash, newPasswordSalt) = CreatePasswordHash(changePasswordDto.NewPassword);


            staffUser.PasswordHash = newPasswordHash;
            staffUser.PasswordSalt = newPasswordSalt;
            staffUser.UpdateAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(staffUser);


            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(staffUser.UserID);

            return true;
        }
        public async Task<bool> ResendOtpAsync(ResendOtpDto resendOtpDto)
        {
            // Kiểm tra xem có temp registration nào cho email này không
            var tempRegistrations = await _unitOfWork.TempRegistrations.GetAllAsync();
            var latestTempReg = tempRegistrations
                .Where(tr => tr.Email == resendOtpDto.Email && !tr.IsUsed)
                .OrderByDescending(tr => tr.CreatedAt)
                .FirstOrDefault();

            if (latestTempReg == null)
            {
                throw new InvalidOperationException("Không tìm thấy yêu cầu đăng ký cho email này. Vui lòng đăng ký lại.");
            }

            // Tạo OTP mới
            var newOtp = new Random().Next(100000, 999999).ToString();

            // Vô hiệu hóa tất cả OTP cũ cho email này
            await _unitOfWork.TempRegistrations.InvalidateTempRegistrationsAsync(resendOtpDto.Email);

            // Tạo temp registration mới với OTP mới
            var newTempRegistration = new TempRegistration
            {
                Email = latestTempReg.Email,
                UserName = latestTempReg.UserName,
                PasswordHash = latestTempReg.PasswordHash,
                PasswordSalt = latestTempReg.PasswordSalt,
                OtpCode = newOtp,
                ExpireAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            await _unitOfWork.TempRegistrations.CreateAsync(newTempRegistration);

            // Gửi email với OTP mới
            await _emailService.SendOtpResendAsync(resendOtpDto.Email, latestTempReg.UserName, newOtp);

            return true;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            // Tìm user theo email hoặc username
            var user = await GetUserByEmailOrUsernameAsync(forgotPasswordDto.EmailOrUsername);

            if (user == null || !user.Status)
            {
                // Không tiết lộ thông tin user có tồn tại hay không
                throw new InvalidOperationException("Nếu email/tài khoản tồn tại, mã OTP đã được gửi đến email của bạn.");
            }

            // Tạo OTP cho reset password
            var resetOtp = new Random().Next(100000, 999999).ToString();

            // Vô hiệu hóa tất cả OTP reset password cũ cho user này
            await _unitOfWork.PasswordResetOtps.InvalidatePasswordResetOtpsAsync(user.Email);

            // Tạo password reset OTP mới
            var passwordResetOtp = new PasswordResetOtp
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                OtpCode = resetOtp,
                ExpireAt = DateTime.UtcNow.AddMinutes(10), // 10 phút cho reset password
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PasswordResetOtps.CreateAsync(passwordResetOtp);

            // Gửi email với OTP reset password
            await _emailService.SendPasswordResetOtpAsync(user.Email, user.UserName, resetOtp);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
          
            var passwordResetOtp = await _unitOfWork.PasswordResetOtps.GetValidPasswordResetOtpAsync(
                resetPasswordDto.Email, resetPasswordDto.OtpCode);

            if (passwordResetOtp == null)
            {
                throw new InvalidOperationException("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }

         
            var user = await _unitOfWork.Users.GetByEmailAsync(resetPasswordDto.Email);
            if (user == null || !user.Status)
            {
                throw new InvalidOperationException("Tài khoản không tồn tại hoặc đã bị khóa.");
            }

       
            passwordResetOtp.IsUsed = true;
            await _unitOfWork.PasswordResetOtps.UpdateAsync(passwordResetOtp);

     
            var (newPasswordHash, newPasswordSalt) = CreatePasswordHash(resetPasswordDto.NewPassword);

     
            user.PasswordHash = newPasswordHash;
            user.PasswordSalt = newPasswordSalt;
            user.UpdateAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);

      
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(user.UserID);

            return true;
        }

        public async Task<AuthResponseDto> LoginGoogleAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleAuthSettings.ClientId }
                };

                if (string.IsNullOrEmpty(_googleAuthSettings.ClientId))
                {
                    throw new InvalidOperationException("Google Client ID is not configured.");
                }

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                var userInfo = new
                {
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    EmailVerified = payload.EmailVerified,
                    Name = payload.Name,
                    Picture = payload.Picture,
                };

                //Check if user exists
                var user = await _unitOfWork.Users.GetByEmailAsync(userInfo.Email);

                if (user != null)
                {
                    if (!user.Status)
                        throw new UnauthorizedAccessException("Tài khoản của bạn không khả dụng, liên hệ với quản trị viên");

                    var (accessToken, refreshToken) = await GenerateTokensAsync(user);

                    return new AuthResponseDto
                    {
                        AccessToken = accessToken.Token,
                        RefreshToken = refreshToken.Token,
                        AccessTokenExpires = accessToken.ExpiresAt,
                        RefreshTokenExpires = refreshToken.ExpiresAt,
                        User = MapToUserInfoDto(user),
                        Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
                    };
                }
                else
                {
                    var newUser = new User
                    {
                        UserID = Guid.NewGuid(),
                        UserName = userInfo.Name.Replace(" ", "") + new Random().Next(1000, 9999),
                        Email = userInfo.Email,
                        PasswordHash = string.Empty,
                        PasswordSalt = string.Empty,
                        JobTitle = "Learner",
                        Interests = string.Empty,
                        BirthDate = DateTime.Now.AddYears(-18),
                        ProfilePictureUrl = userInfo.Picture,
                        Status = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow,
                        LastAcessAt = DateTime.UtcNow,
                        IsEmailConfirmed = userInfo.EmailVerified,
                        MfaEnabled = false,
                        StreakDays = 0
                    };

                    // Save new user
                    await _unitOfWork.Users.CreateAsync(newUser);

                    var defaultRole = await _unitOfWork.Roles.GetByNameAsync("Learner");
                    if (defaultRole != null)
                    {
                        var userRole = new UserRole
                        {
                            UserRoleID = Guid.NewGuid(),
                            UserID = newUser.UserID,
                            RoleID = defaultRole.RoleID
                        };
                        await _unitOfWork.UserRoles.CreateAsync(userRole);
                    }
                    else
                    {
                        throw new InvalidOperationException("Vai trò mặc định không tồn tại, vui lòng liên hệ với quản trị viên.");
                    }

                    await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.UserName);

                    var (accessToken, refreshToken) = await GenerateTokensAsync(newUser);

                    return new AuthResponseDto
                    {
                        AccessToken = accessToken.Token,
                        RefreshToken = refreshToken.Token,
                        AccessTokenExpires = accessToken.ExpiresAt,
                        RefreshTokenExpires = refreshToken.ExpiresAt,
                        User = MapToUserInfoDto(newUser),
                        Roles = new List<string> { "Learner" }
                    };
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Đã xảy ra lỗi trong quá trình xác thực người dùng.", ex);
            }
        }
        #endregion

        private class TokenInfo
        {
            public string Token { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
