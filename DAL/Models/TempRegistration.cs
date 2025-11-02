using DAL.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class TempRegistration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; }
        [Required]
        [StringLength(100)]
        public string UserName { get; set; }
        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; }
        [Required]
        [StringLength(200)]
        public string PasswordSalt { get; set; }
        [Required]
        [StringLength(6)]
        public string OtpCode { get; set; }
        [Required]
        public DateTime ExpireAt { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public bool IsUsed { get; set; } = false;
    }
}
