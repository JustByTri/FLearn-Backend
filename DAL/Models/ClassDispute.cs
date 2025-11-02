using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ClassDispute
    {
        [Key]
        public Guid DisputeID { get; set; }
        [ForeignKey("Class")]
        public Guid ClassID { get; set; }
        [ForeignKey("Enrollment")]
        public Guid EnrollmentID { get; set; }
        [ForeignKey("Student")]
        public Guid StudentID { get; set; }
        [Required]
        [StringLength(500)]
        public string? Reason { get; set; }
        [StringLength(2000)]
        public string? Description { get; set; }
        [Required]
        public DisputeStatus Status { get; set; } = DisputeStatus.Open;
        [StringLength(2000)]
        public string? AdminResponse { get; set; }
        [ForeignKey("ResolvedByAdmin")]
        public Guid? ResolvedByAdminID { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? ResolvedAt { get; set; }
        // Navigation properties
        public virtual TeacherClass? Class { get; set; }
        public virtual ClassEnrollment? Enrollment { get; set; }
        public virtual User? Student { get; set; }
        public virtual User? ResolvedByAdmin { get; set; }
    }
    public enum DisputeStatus
    {
        Open = 0,
        UnderReview = 1,
        Resolved_Refunded = 2,
        Resolved_PartialRefund = 3,
        Resolved_Refused = 4,
        Closed = 5,
        Submmitted = 6
    }
}
