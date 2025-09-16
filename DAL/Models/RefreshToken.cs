using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid RefreshTokenID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [Required]
        [StringLength(500)]
        public string Token { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public bool IsRevoked { get; set; }

        public DateTime? RevokedAt { get; set; }

      
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}
