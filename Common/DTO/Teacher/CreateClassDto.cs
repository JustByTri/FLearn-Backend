using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Common.DTO.Teacher
{
    public class CreateClassDto
    {
        [Required(ErrorMessage = "Tiêu đề lớp học là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        // Program/Level/Language sẽ được suy ra từ TeacherProfile và TeacherProgramAssignment
        // Không cần truyền từ client

        [Required(ErrorMessage = "Ngày diễn ra là bắt buộc")]
        public DateTime ClassDate { get; set; }  // yyyy-MM-dd

        [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc")]
        public TimeSpan StartTime { get; set; }  // HH:mm:ss

        [Required(ErrorMessage = "Thời lượng là bắt buộc")]
        [AllowedDuration]  // Custom validation
        public int DurationMinutes { get; set; }  // 45, 60, 90, hoặc 120

        [Required(ErrorMessage = "Giá học phí là bắt buộc")]
        [Range(0.01, 10000000, ErrorMessage = "Giá học phí phải lớn hơn 0")]
        public decimal PricePerStudent { get; set; }

        /// <summary>
        /// Số học sinh tối thiểu để lớp được diễn ra.
        /// Nếu không đủ số lượng này trước 3 ngày, lớp sẽ bị hủy tự động.
        /// Mặc định: 3, Tối thiểu: 1, Tối đa: 20
        /// </summary>
        [Range(1, 20, ErrorMessage = "Số học sinh tối thiểu phải từ 1 đến 20")]
        public int MinStudents { get; set; } = 3;

        /// <summary>
        /// Sức chứa tối đa của lớp (optional).
        /// Mặc định: 10, Tối đa: 50
        /// </summary>
        [Range(1, 50, ErrorMessage = "Sức chứa tối đa phải từ 1 đến 50")]
        public int Capacity { get; set; } = 10;

        public Guid? ProgramAssignmentId { get; set; }
    }

    public class AllowedDurationAttribute : ValidationAttribute
    {
        private readonly int[] _allowedDurations = { 45, 60, 90, 120 };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is int duration)
            {
                if (_allowedDurations.Contains(duration))
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult("Thời lượng chỉ được phép là 45, 60, 90 hoặc 120 phút");
            }
            return new ValidationResult("Giá trị không hợp lệ");
        }
    }
}
