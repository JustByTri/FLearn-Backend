using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ClassEnrollment
    {
        [Key]
        public Guid EnrollmentID { get; set; }
        [ForeignKey("Class")]
        public Guid ClassID { get; set; }
        [ForeignKey("Student")]
        public Guid StudentID { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }
        [StringLength(100)]
        public string? PaymentTransactionId { get; set; }
        [Required]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
        public DateTime EnrolledAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        // Navigation properties
        public virtual TeacherClass? Class { get; set; }
        public virtual User? Student { get; set; }
        public virtual ICollection<ClassDispute>? Disputes { get; set; }
        public virtual ICollection<RefundRequest>? RefundRequests { get; set; }
    }

    /// <summary>
    /// Trạng thái đăng ký lớp học
    /// </summary>
    public enum EnrollmentStatus
    {
        /// <summary>Chờ thanh toán</summary>
        Pending = 0,

        /// <summary>Đã thanh toán thành công</summary>
        Paid = 1,

        /// <summary>Đã hoàn tiền</summary>
        Refunded = 2,

        /// <summary>Đã hủy</summary>
        Cancelled = 3,

        /// <summary>Đã hoàn thành</summary>
        Completed = 4,

        /// <summary>Đang chờ hoàn tiền (lớp bị hủy, chờ học viên cập nhật thông tin ngân hàng)</summary>
        PendingRefund = 5
    }
}

