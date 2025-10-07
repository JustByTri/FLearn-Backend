using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class User
    {
        [Key]
        public Guid UserID { get; set; }
        [Required]
        [StringLength(100)]
        public string UserName { get; set; }
        [StringLength(100)]
        public string? FullName { get; set; }
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; }
        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; }
        [Required]
        [StringLength(200)]
        public string PasswordSalt { get; set; }
        public DateTime? BirthDate { get; set; }
        public bool Status { get; set; } = true; // Active by default or false for inactive
        [StringLength(300)]
        public string? Avatar { get; set; }
        public bool IsEmailConfirmed { get; set; } = false;
        public Guid? ActiveLanguageId { get; set; }
        [ForeignKey(nameof(ActiveLanguageId))]
        public virtual Language? ActiveLanguage { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<Review>? Reviews { get; set; } = new List<Review>();
        public virtual TeacherApplication? TeacherApplication { get; set; }
        public virtual TeacherProfile? TeacherProfile { get; set; }
        public virtual StaffLanguage? StaffLanguage { get; set; }
        public virtual ICollection<LearnerLanguage> LearnerLanguages { get; set; } = new List<LearnerLanguage>();
        public virtual ICollection<Conversation>? Conversations { get; set; } = new List<Conversation>();
        public virtual ICollection<Purchase>? Purchases { get; set; } = new List<Purchase>();
        public virtual ICollection<Transaction>? Transactions { get; set; } = new List<Transaction>();
    }
}
