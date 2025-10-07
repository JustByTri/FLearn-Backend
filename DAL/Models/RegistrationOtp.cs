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
        public string Email { get; set; }
        [Required]
        [StringLength(6)]
        public string OtpCode { get; set; }
        [Required]
        public DateTime ExpireAt { get; set; }
        public DateTime CreateAt { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
