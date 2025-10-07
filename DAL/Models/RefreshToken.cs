using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid RefreshTokenID { get; set; }
        [Required]
        public Guid UserID { get; set; }
        [ForeignKey(nameof(UserID))]
        public virtual User User { get; set; }
        [Required]
        [StringLength(500)]
        public string Token { get; set; }
        [Required]
        public DateTime ExpiresAt { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
    }
}
