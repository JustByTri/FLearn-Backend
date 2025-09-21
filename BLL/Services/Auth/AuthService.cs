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
                throw new UnauthorizedAccessException("Tài khoản của bạn kh khả dụng, liên hệ với quản trị viên");

            if (!VerifyPassword(loginRequest.Password, user.PasswordHash, user.PasswordSalt))
                throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu của bạn không đúng");

            user.LastAcessAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);

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

        public async Task<bool> RegisterAndSendOtpAsync(TempRegistrationDto registrationDto)
        {
            // Kiểm tra email và username đã tồn tại chưa
            if (await _unitOfWork.Users.IsEmailExistsAsync(registrationDto.Email))
                throw new InvalidOperationException("Email đã được đăng ký bởi tài khoản khác, vui lòng dùng email khác");

            if (await _unitOfWork.Users.IsUsernameExistsAsync(registrationDto.UserName))
                throw new InvalidOperationException("Tên người dùng đã có người sử dụng, hãy thử tên khác nhé");


            var (passwordHash, passwordSalt) = CreatePasswordHash(registrationDto.Password);


            var otp = new Random().Next(100000, 999999).ToString();


            await _unitOfWork.TempRegistrations.InvalidateTempRegistrationsAsync(registrationDto.Email);


            var tempRegistration = new TempRegistration
            {
                Email = registrationDto.Email,
                UserName = registrationDto.UserName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                OtpCode = otp,
                ExpireAt = DateTime.UtcNow.AddMinutes(5)
            };
            await _unitOfWork.TempRegistrations.CreateAsync(tempRegistration);


            await _emailService.SendEmailConfirmationAsync(registrationDto.Email, registrationDto.UserName, otp);
            return true;
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

            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new TokenInfo
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt
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
