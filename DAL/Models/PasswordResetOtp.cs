using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class PasswordResetOtp
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6)] 
        public string OtpCode { get; set; } = string.Empty;

        [Required]
        public DateTime ExpireAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       
        [StringLength(45)]  
        public string? IpAddress { get; set; }

        
        [StringLength(500)]
        public string? UserAgent { get; set; }

      
        public int FailedAttempts { get; set; } = 0;

    
        public DateTime? UsedAt { get; set; }
    }
}
