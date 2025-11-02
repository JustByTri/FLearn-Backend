using DAL.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Language
    {
        [Key]
        public Guid LanguageID { get; set; }
        [Required]
        [StringLength(100)]
        public string LanguageName { get; set; } = string.Empty;
        [Required]
        [StringLength(10)]
        public string LanguageCode { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<LanguageLevel> LanguageLevels { get; set; } = new List<LanguageLevel>();
        public virtual ICollection<Program> Programs { get; set; } = new List<Program>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<TeacherApplication> TeacherApplications { get; set; } = new List<TeacherApplication>();
        public virtual ICollection<TeacherProfile> TeacherProfiles { get; set; } = new List<TeacherProfile>();
        public virtual ICollection<ManagerLanguage> ManagerLanguages { get; set; } = new List<ManagerLanguage>();
        public virtual ICollection<LearnerLanguage> LearnerLanguages { get; set; } = new List<LearnerLanguage>();
        public virtual ICollection<CertificateType> CertificateTypes { get; set; } = new List<CertificateType>();
        public virtual ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
        public virtual ICollection<ConversationSession> ConversationSessions { get; set; } = new List<ConversationSession>();
    }
}
