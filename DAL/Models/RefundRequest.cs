using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class RefundRequest
    {
        [Key]
        public Guid RefundRequestID { get; set; }
        [ForeignKey("ClassEnrollment")]
        public Guid? EnrollmentID { get; set; }
        [Required]
        [ForeignKey("Student")]
        public Guid StudentID { get; set; }
        [ForeignKey("TeacherClass")]
        public Guid? ClassID { get; set; }
        public Guid? PurchaseId { get; set; }
        [ForeignKey(nameof(PurchaseId))]
        public virtual Purchase? Purchase { get; set; }
        public Guid? CourseEnrollmentId { get; set; }
        [ForeignKey(nameof(CourseEnrollmentId))]
        public virtual Enrollment? CourseEnrollment { get; set; }
        [Required]
        public RefundRequestType RequestType { get; set; }
        [Required]
        [StringLength(100)]
        public string? BankName { get; set; }
        [Required]
        [StringLength(50)]
        public string? BankAccountNumber { get; set; }
        [Required]
        [StringLength(100)]
        public string? BankAccountHolderName { get; set; }
        [StringLength(1000)]
        public string? Reason { get; set; }
        [StringLength(500)]
        public string? ProofImageUrl { get; set; }
        [Required]
        public RefundRequestStatus Status { get; set; } = RefundRequestStatus.Pending;
        [StringLength(500)]
        public string? AdminNote { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal RefundAmount { get; set; }
        public DateTime RequestedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? ProcessedAt { get; set; }
        [ForeignKey("ProcessedByAdmin")]
        public Guid? ProcessedByAdminID { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ClassEnrollment ClassEnrollment { get; set; } = null!;
        public virtual User Student { get; set; } = null!;
        public virtual TeacherClass TeacherClass { get; set; } = null!;
        public virtual User ProcessedByAdmin { get; set; } = null!;
    }

    public enum RefundRequestType
    {
        ClassCancelled_InsufficientStudents = 0,
        ClassCancelled_TeacherUnavailable = 1,
        StudentPersonalReason = 2,
        ClassQualityIssue = 3,
        TechnicalIssue = 4,
        Other = 5
    }

    public enum RefundRequestStatus
    {
        Pending = 0,
        UnderReview = 1,
        Approved = 2,
        Rejected = 3,
        Completed = 4,
        Cancelled = 5
    }
}

