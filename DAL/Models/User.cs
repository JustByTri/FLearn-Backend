using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class User
    {
        [Key]
        public Guid UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(200)]
        public string PasswordSalt { get; set; }

        public DateTime? LastAcessAt { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [StringLength(100)]
        public string? Industry { get; set; }

        [Range(0, 10000)]
        public int? StreakDays { get; set; }

        [StringLength(500)]
        public string? Interests { get; set; }

        public DateTime? BirthDate { get; set; }

        public bool Status { get; set; }

        public DateTime UpdateAt { get; set; }

        public bool? MfaEnabled { get; set; }

        [StringLength(300)]
        public string? ProfilePictureUrl { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public ICollection<Language>? Languages { get; set; }
        public ICollection<Role>? Roles { get; set; }
        public ICollection<UserLearningLanguage>? LearningLanguages { get; set; }
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; }
        public virtual ICollection<Roadmap>? Roadmaps { get; set; }
        public virtual ICollection<UserRole>? UserRoles { get; set; }
    }
}
