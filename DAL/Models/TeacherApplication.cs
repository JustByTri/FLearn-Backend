using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class TeacherApplication
    {
        [Key]
        public Guid ApplicationID { get; set; }
        [Required]
        public Guid UserID { get; set; }
        [ForeignKey(nameof(UserID))]
        public virtual User User { get; set; }
        [Required]
        public Guid LanguageID { get; set; }
        [ForeignKey(nameof(LanguageID))]
        public virtual Language Language { get; set; }
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        [Required]
        [StringLength(500)]
        public string Bio { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string Avatar { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string TeachingExperience { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public Guid? ReviewedBy { get; set; }
        [ForeignKey(nameof(ReviewedBy))]
        public virtual StaffLanguage? Staff { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime ReviewedAt { get; set; }
        public virtual ICollection<ApplicationCertType> Certificates { get; set; } = new List<ApplicationCertType>();
    }
}
