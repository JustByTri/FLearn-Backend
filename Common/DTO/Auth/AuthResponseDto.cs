using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpires { get; set; } 
        public DateTime RefreshTokenExpires { get; set; } 
        public UserInfoDto User { get; set; }
        public List<string> Roles { get; set; }

        
        public DateTime AccessTokenExpiresVN => ConvertToVietnamTime(AccessTokenExpires);
        public DateTime RefreshTokenExpiresVN => ConvertToVietnamTime(RefreshTokenExpires);

        private static DateTime ConvertToVietnamTime(DateTime utcTime)
        {
            var vietnamZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, vietnamZone);
        }
    }
    public class UserInfoDto
    {
        public Guid UserID { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessAt { get; set; }
    }
}
