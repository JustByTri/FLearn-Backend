using DAL.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class RegistrationOtp
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = null!;
        [Required]
        [StringLength(6)]
        public string OtpCode { get; set; } = null!;
        [Required]
        public DateTime ExpireAt { get; set; }
        public DateTime CreateAt { get; set; } = TimeHelper.GetVietnamTime();
        public bool IsUsed { get; set; } = false;
    }
}
