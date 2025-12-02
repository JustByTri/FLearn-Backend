using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Teacher
{
    /// <summary>
    /// Request DTO cho việc yêu cầu hủy lớp (áp dụng khi < 3 ngày trước lớp bắt đầu)
    /// </summary>
    public class CancelClassRequestDto
    {
        /// <summary>
        /// Lý do hủy lớp (ốm đau, khẩn cấp, vấn đề cá nhân...)
        /// </summary>
        [Required(ErrorMessage = "Vui lòng cung cấp lý do hủy lớp")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Lý do hủy lớp phải từ 10-1000 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }
}
