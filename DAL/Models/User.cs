using DAL.Helpers;
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
        public string UserName { get; set; } = string.Empty;
        [StringLength(100)]
        public string? FullName { get; set; }
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        [StringLength(200)]
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public bool Status { get; set; } = true;
        [StringLength(300)]
        public string? Avatar { get; set; }
        public bool IsEmailConfirmed { get; set; } = false;
        public Guid? ActiveLanguageId { get; set; }
        [ForeignKey(nameof(ActiveLanguageId))]
        public virtual Language? ActiveLanguage { get; set; }
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? LastLoginAt { get; set; }
        public int DailyConversationLimit { get; set; } = 2;
        public int ConversationsUsedToday { get; set; } = 0;
        public DateTime LastConversationResetDate { get; set; } = TimeHelper.GetVietnamTime();
        public virtual Wallet? Wallet { get; set; }
        public virtual TeacherProfile? TeacherProfile { get; set; }
        public virtual ManagerLanguage? ManagerLanguage { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<TeacherApplication> TeacherApplications { get; set; } = new List<TeacherApplication>();
        public virtual ICollection<LearnerLanguage> LearnerLanguages { get; set; } = new List<LearnerLanguage>();
        public virtual ICollection<PayoutRequest> PayoutRequests { get; set; } = new List<PayoutRequest>();
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public virtual ICollection<GlobalConversationPrompt> GlobalConversationPrompts { get; set; } = new List<GlobalConversationPrompt>();
        public virtual ICollection<UserSubscription>? Subscriptions { get; set; } = new List<UserSubscription>();
        [InverseProperty("Student")]
        public virtual ICollection<RefundRequest> RefundRequestsAsStudent { get; set; } = new List<RefundRequest>();
        [InverseProperty("ProcessedByAdmin")]
        public virtual ICollection<RefundRequest> RefundRequestsProcessed { get; set; } = new List<RefundRequest>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<GlobalConversationPrompt> CreatedPrompts { get; set; } = new List<GlobalConversationPrompt>();
        public virtual ICollection<GlobalConversationPrompt> ModifiedPrompts { get; set; } = new List<GlobalConversationPrompt>();
    }
}
