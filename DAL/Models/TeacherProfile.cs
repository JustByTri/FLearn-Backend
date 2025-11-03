using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class TeacherProfile
    {
        [Key]
        public Guid TeacherId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; } = null!;
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        [Required]
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
        [StringLength(50)]
        public string ProficiencyCode { get; set; } = null!;
        [Required]
        public int ProficiencyOrder { get; set; }
        public double AverageRating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;
        [Required]
        [StringLength(500)]
        public string MeetingUrl { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual Wallet? Wallet { get; set; }
        public virtual ICollection<TeacherBankAccount> TeacherBankAccounts { get; set; } = new List<TeacherBankAccount>();
        public virtual ICollection<PayoutRequest> PayoutRequests { get; set; } = new List<PayoutRequest>();
        public virtual ICollection<TeacherProgramAssignment> TeacherProgramAssignments { get; set; } = new List<TeacherProgramAssignment>();
        public virtual ICollection<ExerciseGradingAssignment> ExerciseGradingAssignments { get; set; } = new List<ExerciseGradingAssignment>();
        public virtual ICollection<TeacherEarningAllocation> TeacherEarningAllocations { get; set; } = new List<TeacherEarningAllocation>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
        public virtual ICollection<TeacherReview> TeacherReviews { get; set; } = new List<TeacherReview>();
    }
}
