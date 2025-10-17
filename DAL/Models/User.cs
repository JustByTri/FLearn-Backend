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
        public int DailyConversationLimit { get; set; } = 2;
        public int ConversationsUsedToday { get; set; } = 0;
        public DateTime LastConversationResetDate { get; set; } = DateTime.UtcNow;
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<Review>? Reviews { get; set; } = new List<Review>();
        public virtual ICollection<TeacherApplication>? TeacherApplications { get; set; } = new List<TeacherApplication>();
        public virtual TeacherProfile? TeacherProfile { get; set; }
        public virtual StaffLanguage? StaffLanguage { get; set; }
        public virtual ICollection<LearnerLanguage> LearnerLanguages { get; set; } = new List<LearnerLanguage>();
        public virtual ICollection<Conversation>? Conversations { get; set; } = new List<Conversation>();
        public virtual ICollection<Purchase>? Purchases { get; set; } = new List<Purchase>();
        public virtual ICollection<Transaction>? Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<ConversationTask>? ConversationTasks { get; set; } = new List<ConversationTask>();
        public virtual ICollection<UserSubscription>? Subscriptions { get; set; } = new List<UserSubscription>();
    }
}
