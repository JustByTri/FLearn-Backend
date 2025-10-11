using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Language
    {
        [Key]
        public Guid LanguageID { get; set; }
        [Required]
        [StringLength(100)]
        public string LanguageName { get; set; }
        [Required]
        [StringLength(10)]
        public string LanguageCode { get; set; }
        public bool Status { get; set; } = true; // Active by default or false for inactive
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<LanguageLevel> LanguageLevels { get; set; } = new List<LanguageLevel>(); // Proficiency levels associated with this language
        public virtual ICollection<User>? Users { get; set; } = new List<User>(); // Users who have this as their active language
        public virtual ICollection<StaffLanguage>? StaffLanguages { get; set; } // Staff members associated with this language
        public virtual ICollection<TeacherApplication>? TeacherApplications { get; set; } // Teacher applications for this language
        public virtual ICollection<TeacherProfile>? TeacherProfiles { get; set; } // Teacher profiles for this language
        public virtual ICollection<CertificateType> CertificateTypes { get; set; } = new List<CertificateType>(); // Certificate types associated with this language
        public virtual ICollection<LearnerLanguage>? LearnerLanguages { get; set; } // Learners associated with this language
        public virtual ICollection<Achievement>? Achievements { get; set; } // Achievements associated with this language
        public virtual ICollection<Conversation>? Conversations { get; set; } // Conversations in this language
        public virtual ICollection<Course>? Courses { get; set; }
        public virtual ICollection<GlobalConversationPrompt>? GlobalConversationPrompts { get; set; } // Global conversation prompts in this language
    }
}
