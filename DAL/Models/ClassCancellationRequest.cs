using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DAL.Helpers;

namespace DAL.Models
{
    /// <summary>
    /// Yêu cầu hủy lớp từ giáo viên (áp dụng khi hủy trong vòng 3 ngày trước khi bắt đầu)
    /// </summary>
    public class ClassCancellationRequest
    {
        [Key]
        public Guid CancellationRequestId { get; set; }

        /// <summary>
        /// ID lớp học cần hủy
        /// </summary>
        [Required]
        [ForeignKey("TeacherClass")]
        public Guid ClassId { get; set; }

        /// <summary>
        /// ID giáo viên yêu cầu hủy
        /// </summary>
        [Required]
        [ForeignKey("Teacher")]
        public Guid TeacherId { get; set; }

        /// <summary>
        /// Lý do hủy lớp (ốm đau, khẩn cấp, v.v.)
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái yêu cầu: Pending, Approved, Rejected
        /// </summary>
        [Required]
        public CancellationRequestStatus Status { get; set; } = CancellationRequestStatus.Pending;

        /// <summary>
        /// ID Manager xử lý yêu cầu
        /// </summary>
        [ForeignKey("ProcessedByManager")]
        public Guid? ProcessedByManagerId { get; set; }

        /// <summary>
        /// Ghi chú từ Manager khi duyệt/từ chối
        /// </summary>
        [StringLength(1000)]
        public string? ManagerNote { get; set; }

        /// <summary>
        /// Thời điểm giáo viên gửi yêu cầu
        /// </summary>
        public DateTime RequestedAt { get; set; } = TimeHelper.GetVietnamTime();

        /// <summary>
        /// Thời điểm Manager xử lý
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        // Navigation Properties
        public virtual TeacherClass TeacherClass { get; set; } = null!;
        public virtual User Teacher { get; set; } = null!;
        public virtual User? ProcessedByManager { get; set; }
    }

    /// <summary>
    /// Trạng thái yêu cầu hủy lớp
    /// </summary>
    public enum CancellationRequestStatus
    {
        /// <summary>Chờ Manager duyệt</summary>
        Pending = 0,

        /// <summary>Manager đã chấp nhận</summary>
        Approved = 1,

        /// <summary>Manager từ chối</summary>
        Rejected = 2
    }
}
